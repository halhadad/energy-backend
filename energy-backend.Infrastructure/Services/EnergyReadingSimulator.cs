using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using energy_backend.Core.Common;
using energy_backend.Data;
using energy_backend.Entities;
using energy_backend.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class EnergyReadingSimulator : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EnergyReadingSimulator> _logger;
        private readonly Random _rng = new Random();

        public EnergyReadingSimulator(IServiceScopeFactory scopeFactory, ILogger<EnergyReadingSimulator> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
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

                    // 1) Compute this slot (floor to 5s)
                    var nowSlot = TimeSlots.FloorTo5sUtc(DateTime.UtcNow);

                    // 2) Load devices
                    var devices = await db.Devices.AsNoTracking().Select(d => d.DeviceId).ToListAsync(stoppingToken);
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
                    var toInsert = new List<EnergyReading>(capacity: Math.Max(8, devices.Count - existingSet.Count));
                    foreach (var deviceId in devices)
                    {
                        if (existingSet.Contains(deviceId)) continue;

                        toInsert.Add(new EnergyReading
                        {
                            EnergyReadingId = Guid.NewGuid(),
                            DeviceId = deviceId,
                            Timestamp = nowSlot,
                            EnergyValue = (float)Math.Round(_rng.NextDouble() * 0.02, 5) // ~0.00000..0.02000
                        });
                    }

                    if (toInsert.Count > 0)
                    {
                        await db.EnergyReadings.AddRangeAsync(toInsert, stoppingToken);
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Simulator inserted {Count} readings for {Slot}", toInsert.Count, nowSlot);
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
