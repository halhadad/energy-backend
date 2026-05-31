using energy_backend.Application.Interfaces;
using energy_backend.Core.Common;
using energy_backend.Core.Entities;
using energy_backend.Core.Enums;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace energy_backend.Infrastructure.Services
{
    public class MockDataSimulationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MockDataSimulationWorker> _logger;
        private readonly Random _rng = new();
        private readonly Dictionary<Guid, float> _deviceBaseWatts = new();
        private readonly Dictionary<Guid, float> _deviceBasePf = new();
        private const float NominalVoltage = 230f;
        public MockDataSimulationWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<MockDataSimulationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DelayUntilNextSlot(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ── SCOPE 1: read devices + write raw EnergyReadings ──────────────────
                    // This scope is disposed before the coordinator scope opens.
                    // The two scopes never share a DbContext instance, which eliminates
                    // the "second operation started before previous completed" error.
                    List<EnergyReading> newReadings;
                    List<Device> devices;
                    using (var dbScope = _scopeFactory.CreateScope())
                    {
                        var db = dbScope.ServiceProvider.GetRequiredService<EnergyDbContext>();
                        var nowSlot = TimeSlots.FloorTo5sUtc(DateTime.UtcNow);
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
                                    : (float)(_rng.NextDouble() * 450.0 + 50.0);
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
                            newReadings.Add(new EnergyReading
                            {
                                EnergyReadingId = Guid.NewGuid(),
                                OrgId = device.OrganisationId,
                                DeviceId = device.DeviceId,
                                Timestamp = nowSlot,
                                ActivePowerWatts = (float)Math.Round(activePower, 2),
                                VoltageVolts = (float)Math.Round(voltage, 1),
                                CurrentAmps = (float)Math.Round(current, 3),
                                PowerFactor = (float)Math.Round(pf, 3),
                                ActiveEnergyKwh = activePower * 5f / 3_600_000f
                            });
                        }
                        if (newReadings.Count == 0)
                        {
                            await DelayUntilNextSlot(stoppingToken);
                            continue;
                        }
                        await db.EnergyReadings.AddRangeAsync(newReadings, stoppingToken);
                        await db.SaveChangesAsync(stoppingToken);
                        // dbScope disposed here — db is released before coordinator opens
                    }
                    _logger.LogDebug(
                        "Simulator: {Count} readings at {Slot} — watts: {Watts}",
                        newReadings.Count,
                        newReadings.FirstOrDefault()?.Timestamp,
                        string.Join(", ", newReadings.Select(r =>
                            $"{r.ActivePowerWatts:F0}W/{r.VoltageVolts:F0}V/{r.CurrentAmps:F2}A/PF={r.PowerFactor:F2}")));
                    // ── SCOPE 2: aggregate + notify — completely separate DbContext ────────
                    using (var coordScope = _scopeFactory.CreateScope())
                    {
                        var coordinator = coordScope.ServiceProvider.GetRequiredService<IMockDataAggregationService>();
                        var byOrg = newReadings
                            .GroupBy(r => devices.First(d => d.DeviceId == r.DeviceId).OrganisationId);
                        foreach (var orgGroup in byOrg)
                        {
                            foreach (var reading in orgGroup)
                                await coordinator.UpsertBucketsAsync(reading);
                            await coordinator.NotifyOrgAsync(
                                orgGroup.First().DeviceId,
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
                    _logger.LogError(ex, "Simulator: unhandled error");
                }
                await DelayUntilNextSlot(stoppingToken);
            }
        }
        private (float activePower, float voltage, float current, float pf)
            SimulateReading(float ratedW, float basePf, DateTime utcTime)
        {
            var voltageNoise = (_rng.NextDouble() * 0.04) - 0.02;
            var voltage = (float)(NominalVoltage * (1.0 + voltageNoise));
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
            var activePower = (float)Math.Max(0.0, ratedW * loadFactor * (1.0 + powerNoise));
            var pfNoise = (_rng.NextDouble() * 0.06) - 0.03;
            var pf = (float)Math.Clamp(basePf + pfNoise, 0.50, 1.00);
            var current = (voltage > 0 && pf > 0) ? activePower / (voltage * pf) : 0f;
            return (activePower, voltage, current, pf);
        }
        private static float BasePowerFactorForDeviceType(DeviceType type) => type switch
        {
            DeviceType.HVAC or DeviceType.IndustrialLoad => 0.80f,
            DeviceType.Refrigeration => 0.82f,
            DeviceType.Lighting => 0.90f,
            DeviceType.ITEquipment or DeviceType.OfficeEquipment => 0.95f,
            DeviceType.EVCharger or DeviceType.SolarPV or DeviceType.BatteryStorage => 0.98f,
            _ => 0.92f
        };
        private static async Task DelayUntilNextSlot(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var next = new DateTime(
                now.Ticks - (now.Ticks % TimeSpan.FromSeconds(5).Ticks) + TimeSpan.FromSeconds(5).Ticks,
                DateTimeKind.Utc);
            var delay = next - now;
            if (delay.TotalMilliseconds < 0) delay = TimeSpan.Zero;
            await Task.Delay(delay, ct);
        }
    }
}