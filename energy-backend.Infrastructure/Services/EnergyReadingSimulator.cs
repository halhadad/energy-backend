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
    /// Simulates a real IoT power meter (e.g. Shelly 3EM) sending active power
    /// readings every 5 seconds per device.
    ///
    /// EnergyReading.PowerWatts = instantaneous active power in Watts.
    ///
    /// Simulation model:
    ///   basePower  = device.RatedPowerWatts  (or random 50–500 W if not set)
    ///   loadFactor = time-of-day curve (0.15 overnight → 1.0 peak)
    ///   noise      = ±25% random jitter so charts look alive
    ///   PowerWatts = basePower × loadFactor × (1 + noise)
    ///
    /// This means the "Current Consumption" card will read close to the sum of
    /// all devices' RatedPowerWatts during peak hours, with realistic variation.
    /// </summary>
    public class EnergyReadingSimulator : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EnergyReadingSimulator> _logger;
        private readonly Random _rng = new Random();

        // Stable base watts per device (= RatedPowerWatts, assigned once).
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

                    var existingIds = await db.EnergyReadings
                        .AsNoTracking()
                        .Where(r => r.Timestamp == nowSlot)
                        .Select(r => r.DeviceId)
                        .ToHashSetAsync(stoppingToken);

                    var newReadings = new List<EnergyReading>();

                    foreach (var device in devices)
                    {
                        if (existingIds.Contains(device.DeviceId)) continue;

                        // Use RatedPowerWatts as stable base; assign random default if unset
                        if (!_deviceBaseWatts.TryGetValue(device.DeviceId, out var baseW))
                        {
                            if (device.RatedPowerWatts <= 0)
                            {
                                baseW = (float)(_rng.NextDouble() * 450.0 + 50.0); // 50–500 W
                                _logger.LogWarning(
                                    "Device '{Name}' ({Id}) has no RatedPowerWatts; using {W:F0} W",
                                    device.Name, device.DeviceId, baseW);
                            }
                            else
                            {
                                baseW = device.RatedPowerWatts;
                            }
                            _deviceBaseWatts[device.DeviceId] = baseW;
                        }

                        var watts = SimulateWatts(baseW, nowSlot);

                        newReadings.Add(new EnergyReading
                        {
                            EnergyReadingId = Guid.NewGuid(),
                            DeviceId = device.DeviceId,
                            Timestamp = nowSlot,
                            PowerWatts = (float)Math.Round(watts, 2),
                        });
                    }

                    if (newReadings.Count == 0)
                    {
                        await DelayUntilNextSlot(stoppingToken);
                        continue;
                    }

                    await db.EnergyReadings.AddRangeAsync(newReadings, stoppingToken);
                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogDebug("Simulator: {Count} readings at {Slot} — watts: {Watts}",
                        newReadings.Count, nowSlot,
                        string.Join(", ", newReadings.Select(r => $"{r.PowerWatts:F0}W")));

                    // Upsert all devices first, then fire ONE SignalR message per org
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

        /// <summary>
        /// Simulates realistic instantaneous power for a device at a given time.
        ///
        /// Time-of-day load curve keeps values anchored close to rated power
        /// during business/evening hours and drops them overnight — matching
        /// real household and office consumption patterns.
        ///
        /// Noise: ±25% uniform jitter so readings look like a real sensor.
        /// </summary>
        private float SimulateWatts(float ratedW, DateTime utcTime)
        {
            var hour = utcTime.Hour;
            double loadFactor = hour switch
            {
                >= 0 and < 5 => 0.15,                          // Deep night — almost off
                >= 5 and < 7 => 0.30 + (hour - 5) * 0.15,     // Early morning ramp
                >= 7 and < 9 => 0.65 + (hour - 7) * 0.10,     // Morning ramp
                >= 9 and < 12 => 0.85,                          // Mid-morning
                >= 12 and < 14 => 0.75,                         // Lunch dip
                >= 14 and < 17 => 0.85,                         // Afternoon
                >= 17 and < 20 => 1.00,                         // Evening peak
                >= 20 and < 22 => 0.80,                         // Wind-down
                _ => 0.50                                        // Late night
            };

            // ±25% noise — wide enough to look interesting, narrow enough
            // that the value stays recognizably close to rated power
            var noise = (_rng.NextDouble() * 0.50) - 0.25;
            var watts = ratedW * loadFactor * (1.0 + noise);

            return (float)Math.Max(0.0, watts);
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