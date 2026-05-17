using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using energy_backend.Core.Common;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using energy_backend.Application.Services;

namespace energy_backend.Infrastructure.Services
{
    /// <summary>
    /// Simulates a real IoT power meter (e.g. Shelly 3EM).
    ///
    /// A real Shelly pushes ACTIVE POWER in Watts every few seconds — not kWh,
    /// not accumulated energy. EnergyReading.EnergyValue therefore stores Watts.
    ///
    /// The aggregation layer converts W → kWh when writing time-series buckets.
    /// The "Current Consumption" card reads AverageWatts directly — no conversion.
    /// </summary>
    public class EnergyReadingSimulator : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EnergyReadingSimulator> _logger;
        private readonly Random _rng = new Random();

        // Stable base watts per device, assigned once per process lifetime.
        // Real sensors change gradually; we model that with small jitter around
        // a fixed base rather than a new random every 5 seconds.
        private readonly Dictionary<Guid, float> _deviceBaseWatts = new();

        public EnergyReadingSimulator(IServiceScopeFactory scopeFactory, ILogger<EnergyReadingSimulator> logger)
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
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
                    var coordinator = scope.ServiceProvider.GetRequiredService<IAggregationCoordinatorService>();

                    var nowSlot = TimeSlots.FloorTo5sUtc(DateTime.UtcNow);

                    var devices = await db.Devices
                        .Include(d => d.Organisation)
                        .AsNoTracking()
                        .ToListAsync(stoppingToken);

                    if (devices.Count == 0)
                    {
                        await DelayUntilNextSlot(stoppingToken);
                        continue;
                    }

                    var existingDeviceIds = await db.EnergyReadings
                        .AsNoTracking()
                        .Where(r => r.Timestamp == nowSlot)
                        .Select(r => r.DeviceId)
                        .ToHashSetAsync(stoppingToken);

                    var newReadings = new List<EnergyReading>();

                    foreach (var device in devices)
                    {
                        if (existingDeviceIds.Contains(device.DeviceId)) continue;

                        // Use device.EnergyConsumption (Rated Power in Watts, entered
                        // by the user) as the simulation baseline. Fall back to a
                        // sensible random value if the user left it blank.
                        if (!_deviceBaseWatts.TryGetValue(device.DeviceId, out var baseW))
                        {
                            baseW = device.EnergyConsumption > 0
                                ? device.EnergyConsumption
                                : (float)(_rng.NextDouble() * 480.0 + 20.0);
                            _deviceBaseWatts[device.DeviceId] = baseW;
                        }

                        // ±15% jitter simulates normal load variation
                        var jitter = (float)((_rng.NextDouble() * 0.30) - 0.15);
                        var instantWatts = Math.Max(0f, baseW * (1f + jitter));

                        // Store WATTS — this is what the IoT device actually reports.
                        newReadings.Add(new EnergyReading
                        {
                            EnergyReadingId = Guid.NewGuid(),
                            DeviceId = device.DeviceId,
                            Timestamp = nowSlot,
                            EnergyValue = (float)Math.Round(instantWatts, 2),
                        });
                    }

                    if (newReadings.Count == 0)
                    {
                        await DelayUntilNextSlot(stoppingToken);
                        continue;
                    }

                    await db.EnergyReadings.AddRangeAsync(newReadings, stoppingToken);
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Simulator: {Count} W-readings at {Slot}", newReadings.Count, nowSlot);

                    // Group by org so we call NotifyOrgAsync ONCE per org,
                    // not once per device (which caused the flashing).
                    var devicesByOrg = newReadings
                        .GroupBy(r => devices.First(d => d.DeviceId == r.DeviceId).OrganisationId);

                    foreach (var orgGroup in devicesByOrg)
                    {
                        foreach (var reading in orgGroup)
                            await coordinator.UpsertBucketsAsync(reading);

                        await coordinator.NotifyOrgAsync(
                            orgGroup.First().DeviceId,
                            orgGroup.First().Timestamp);
                    }
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Simulator: unique constraint; skipping slot.");
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Simulator: unhandled error");
                }

                await DelayUntilNextSlot(stoppingToken);
            }
        }

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