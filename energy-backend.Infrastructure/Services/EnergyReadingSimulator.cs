using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using energy_backend.Core.Common;
using energy_backend.Core.Entities;
using energy_backend.Data;
using energy_backend.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using energy_backend.Application.Services; // Added for IAggregationCoordinatorService

namespace energy_backend.Infrastructure.Services
{
    public class EnergyReadingSimulator : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EnergyReadingSimulator> _logger;
        private readonly Random _rng = new Random();
        // REMOVED: invalid scoped dependency in singleton (was causing crash)
        // private readonly IAggregationCoordinatorService _aggregationCoordinatorService; // Injected

        public EnergyReadingSimulator(
            IServiceScopeFactory scopeFactory,
            ILogger<EnergyReadingSimulator> logger)
        // REMOVED PARAM: IAggregationCoordinatorService aggregationCoordinatorService
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            // REMOVED assignment
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Align first tick to next 5s boundary
            await DelayUntilNextSlot(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();

                    // IMPORTANT: resolve scoped service inside scope (NOT constructor)
                    var aggregationCoordinatorService =
                        scope.ServiceProvider.GetRequiredService<IAggregationCoordinatorService>();

                    // 1) Compute this slot (floor to 5s)
                    var nowSlot = TimeSlots.FloorTo5sUtc(DateTime.UtcNow);

                    // 2) Load devices with their organizations for OrgId
                    var devices = await db.Devices
                                        .Include(d => d.Organisation) // Include Organisation to get OrgId
                                        .AsNoTracking()
                                        .ToListAsync(stoppingToken);

                    if (devices.Count == 0)
                    {
                        await DelayUntilNextSlot(stoppingToken);
                        continue;
                    }

                    // 3) Query existing readings for THIS slot for all devices (single query)
                    var existingForSlot = await db.EnergyReadings
                        .AsNoTracking()
                        .Where(r => r.Timestamp == nowSlot)
                        .Select(r => r.DeviceId)
                        .ToListAsync(stoppingToken);

                    var existingSet = new HashSet<Guid>(existingForSlot);

                    // 4) Create readings only for missing devices
                    var newReadings = new List<EnergyReading>();
                    foreach (var device in devices)
                    {
                        if (existingSet.Contains(device.DeviceId)) continue;

                        var newReading = new EnergyReading
                        {
                            EnergyReadingId = Guid.NewGuid(),
                            DeviceId = device.DeviceId,
                            Timestamp = nowSlot,
                            EnergyValue = (float)Math.Round(_rng.NextDouble() * 0.02, 5), // ~0.00000..0.02000
                            // Device = device // Attach device to ensure OrgId is available for aggregation
                        };
                        newReadings.Add(newReading);
                    }

                    if (newReadings.Count > 0)
                    {
                        await db.EnergyReadings.AddRangeAsync(newReadings, stoppingToken);
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Simulator inserted {Count} readings for {Slot}", newReadings.Count, nowSlot);

                        // Process new readings through the aggregation coordinator
                        foreach (var newReading in newReadings)
                        {
                            await aggregationCoordinatorService.ProcessEnergyReadingAsync(newReading);
                        }
                    }
                }
                catch (DbUpdateException ex)
                {
                    // Unique index collisions (rare if the query is accurate); ignore and continue
                    _logger.LogWarning(ex, "Simulator hit unique constraint; continuing.");
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Simulator error");
                }

                // 5) Wait until next 5s boundary
                await DelayUntilNextSlot(stoppingToken);
            }
        }

        private static async Task DelayUntilNextSlot(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var next = new DateTime(now.Ticks - (now.Ticks % TimeSpan.FromSeconds(5).Ticks) + TimeSpan.FromSeconds(5).Ticks, DateTimeKind.Utc);
            var delay = next - now;
            if (delay.TotalMilliseconds < 0) delay = TimeSpan.Zero;
            await Task.Delay(delay, ct);
        }
    }
}