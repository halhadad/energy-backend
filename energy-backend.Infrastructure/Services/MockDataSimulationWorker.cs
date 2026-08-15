using energy_backend.Application.Interfaces;
using energy_backend.Core.Common;
using energy_backend.Core.Entities;
using energy_backend.Core.Enums;
using energy_backend.Infrastructure.Configuration;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace energy_backend.Infrastructure.Services;

// generates one reading per device each interval and keeps the minute buckets up to date.
// live broadcasting is done separately by LiveBroadcastWorker
public class MockDataSimulationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MockDataSimulationWorker> _logger;
    private readonly EnergyWorkerOptions _options;
    private readonly Random _rng = new();
    private readonly Dictionary<Guid, double> _deviceBaseWatts = new();
    private readonly Dictionary<Guid, double> _deviceBasePf = new();

    public MockDataSimulationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EnergyWorkerOptions> options,
        ILogger<MockDataSimulationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MockDataSimulationWorker started (interval: {Interval}s).",
            _options.SimulatorIntervalSeconds);

        await DelayUntilNextSlot(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<EnergyReading> newReadings;
                List<Device> devices;

                using (var dbScope = _scopeFactory.CreateScope())
                {
                    var db = dbScope.ServiceProvider.GetRequiredService<EnergyDbContext>();
                    var nowSlot = TimeSlots.FloorToIntervalUtc(
                        DateTime.UtcNow, _options.SimulatorIntervalSeconds);

                    devices = await db.Devices
                        .Include(d => d.Organisation)
                        .AsNoTracking()
                        .ToListAsync(stoppingToken);

                    if (devices.Count == 0)
                    {
                        await DelayUntilNextSlot(stoppingToken);
                        continue;
                    }

                    var existingIds = await db.EnergyReadings
                        .AsNoTracking()
                        .Where(r => r.Timestamp == nowSlot)
                        .Select(r => r.DeviceId)
                        .ToHashSetAsync(stoppingToken);

                    newReadings = new List<EnergyReading>();

                    foreach (var device in devices)
                    {
                        if (existingIds.Contains(device.DeviceId)) continue;

                        if (!_deviceBaseWatts.TryGetValue(device.DeviceId, out var baseW))
                        {
                            baseW = device.RatedPowerWatts > 0
                                ? device.RatedPowerWatts
                                : _rng.NextDouble() * 450.0 + 50.0;

                            if (device.RatedPowerWatts <= 0)
                                _logger.LogWarning(
                                    "Device '{Name}' ({Id}) has no RatedPowerWatts; using {W:F0} W",
                                    device.Name, device.DeviceId, baseW);

                            _deviceBaseWatts[device.DeviceId] = baseW;
                        }

                        if (!_deviceBasePf.TryGetValue(device.DeviceId, out var basePf))
                        {
                            basePf = BasePowerFactorForDeviceType(device.Type);
                            _deviceBasePf[device.DeviceId] = basePf;
                        }

                        var (activePower, voltage, current, pf) = SimulateReading(baseW, basePf, nowSlot);

                        // energy for this interval, derived from the interval length so the cadence can change freely
                        var intervalKwh = activePower * _options.SimulatorIntervalSeconds / 3_600_000d;

                        newReadings.Add(new EnergyReading
                        {
                            EnergyReadingId = Guid.NewGuid(),
                            OrgId = device.OrganisationId,
                            DeviceId = device.DeviceId,
                            Timestamp = nowSlot,
                            ActivePowerWatts = activePower,
                            VoltageVolts = voltage,
                            CurrentAmps = current,
                            PowerFactor = pf,
                            ActiveEnergyKwh = intervalKwh
                        });
                    }

                    if (newReadings.Count == 0)
                    {
                        await DelayUntilNextSlot(stoppingToken);
                        continue;
                    }

                    await db.EnergyReadings.AddRangeAsync(newReadings, stoppingToken);
                    await db.SaveChangesAsync(stoppingToken);
                }

                _logger.LogDebug(
                    "Simulator: {Count} readings at {Slot}",
                    newReadings.Count,
                    newReadings[0].Timestamp);

                // update the minute buckets for these readings, then push chart updates over signalr
                using (var coordScope = _scopeFactory.CreateScope())
                {
                    var coordinator = coordScope.ServiceProvider
                        .GetRequiredService<IMockDataAggregationService>();

                    foreach (var reading in newReadings)
                        await coordinator.UpsertBucketsAsync(reading);

                    var byOrg = newReadings
                        .GroupBy(r => devices.First(d => d.DeviceId == r.DeviceId).OrganisationId);

                    foreach (var orgGroup in byOrg)
                    {
                        await coordinator.NotifyOrgAsync(
                            orgGroup.Key,
                            orgGroup.Select(r => r.DeviceId),
                            orgGroup.First().Timestamp);
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Simulator: duplicate slot; skipping.");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simulator: unhandled error in tick loop.");
            }

            await DelayUntilNextSlot(stoppingToken);
        }
    }

    private (double activePower, double voltage, double current, double pf)
        SimulateReading(double ratedW, double basePf, DateTime utcTime)
    {
        var voltageNoise = (_rng.NextDouble() * 0.04) - 0.02;
        var voltage = _options.NominalVoltage * (1.0 + voltageNoise);

        var hour = utcTime.Hour;
        double loadFactor = hour switch
        {
            >= 0 and < 5 => 0.15,
            >= 5 and < 7 => 0.30 + (hour - 5) * 0.15,
            >= 7 and < 9 => 0.65 + (hour - 7) * 0.10,
            >= 9 and < 12 => 0.85,
            >= 12 and < 14 => 0.75,
            >= 14 and < 17 => 0.85,
            >= 17 and < 20 => 1.00,
            >= 20 and < 22 => 0.80,
            _ => 0.50
        };

        var powerNoise = (_rng.NextDouble() * 0.40) - 0.20;
        var activePower = Math.Max(0.0, ratedW * loadFactor * (1.0 + powerNoise));

        var pfNoise = (_rng.NextDouble() * 0.06) - 0.03;
        var pf = Math.Clamp(basePf + pfNoise, 0.50, 1.00);
        var current = (voltage > 0 && pf > 0) ? activePower / (voltage * pf) : 0d;

        return (activePower, voltage, current, pf);
    }

    private static double BasePowerFactorForDeviceType(DeviceType type) => type switch
    {
        DeviceType.HVAC or DeviceType.IndustrialLoad => 0.80,
        DeviceType.Refrigeration => 0.82,
        DeviceType.Lighting => 0.90,
        DeviceType.ITEquipment or DeviceType.OfficeEquipment => 0.95,
        DeviceType.EVCharger or DeviceType.SolarPV or DeviceType.BatteryStorage => 0.98,
        _ => 0.92
    };

    // sleep until the next aligned slot boundary
    private async Task DelayUntilNextSlot(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var intervalTicks = TimeSpan.FromSeconds(_options.SimulatorIntervalSeconds).Ticks;
        var next = new DateTime(
            now.Ticks - (now.Ticks % intervalTicks) + intervalTicks,
            DateTimeKind.Utc);
        var delay = next - now;
        if (delay.TotalMilliseconds < 0) delay = TimeSpan.Zero;
        await Task.Delay(delay, ct);
    }
}
