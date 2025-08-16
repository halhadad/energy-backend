using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using energy_backend.Core.Entities;
using energy_backend.Data;
using energy_backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Seeding
{
    public static class SeedData
    {
        public static async Task SeedEnergyReadingsEvery5SecAsync(EnergyDbContext context)
        {
            var devices = await context.Devices.AsNoTracking().ToListAsync();
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var rng = new Random();

            foreach (var device in devices)
            {
                // Find the last timestamp for this device
                var lastReading = await context.EnergyReadings
                    .Where(r => r.DeviceId == device.DeviceId)
                    .OrderByDescending(r => r.Timestamp)
                    .Select(r => r.Timestamp)
                    .FirstOrDefaultAsync();

                // Start right after the last record or from the start date
                var currentTime = lastReading != default
                    ? lastReading.AddSeconds(5)
                    : startDate;

                var readings = new List<EnergyReading>();
                while (currentTime <= endDate)
                {
                    // Avoid accidental duplicate timestamps
                    bool exists = await context.EnergyReadings
                        .AnyAsync(r => r.DeviceId == device.DeviceId && r.Timestamp == currentTime);

                    if (!exists)
                    {
                        readings.Add(new EnergyReading
                        {
                            EnergyReadingId = Guid.NewGuid(),
                            DeviceId = device.DeviceId,
                            EnergyValue = (float)Math.Round(rng.NextDouble() * 0.01, 5),
                            Timestamp = currentTime
                        });
                    }
                    currentTime = currentTime.AddSeconds(5);
                }

                // Chunk insert
                const int chunkSize = 5000;
                for (int i = 0; i < readings.Count; i += chunkSize)
                {
                    var chunk = readings.Skip(i).Take(chunkSize).ToList();
                    await context.EnergyReadings.AddRangeAsync(chunk);
                    await context.SaveChangesAsync();
                }
            }
        }

        public static async Task SeedAggregatedEnergyDbAsync(EnergyDbContext context)
        {
            var devices = await context.Devices.AsNoTracking().ToListAsync();
            var startDate = DateTime.UtcNow.AddDays(-7).Date;
            var endDate = DateTime.UtcNow;
            var rng = new Random();

            foreach (var device in devices)
            {
                var lastAggregate = await context.AggregatedEnergies
                    .Where(a => a.DeviceId == device.DeviceId)
                    .OrderByDescending(a => a.PeriodStartTime)
                    .Select(a => a.PeriodStartTime)
                    .FirstOrDefaultAsync();

                var currentTime = lastAggregate != default
                    ? lastAggregate.AddHours(1)
                    : startDate;

                var aggregates = new List<AggregatedEnergy>();

                while (currentTime <= endDate)
                {
                    // Avoid duplicate periods
                    bool exists = await context.AggregatedEnergies
                        .AnyAsync(a => a.DeviceId == device.DeviceId && a.PeriodStartTime == currentTime);

                    if (!exists)
                    {
                        aggregates.Add(new AggregatedEnergy
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = device.DeviceId,
                            PeriodStartTime = currentTime,
                            TotalKwh = (float)Math.Round(rng.NextDouble() * 3, 4)
                        });
                    }

                    currentTime = currentTime.AddHours(1);
                }

                // Chunk insert
                const int chunkSize = 1000;
                for (int i = 0; i < aggregates.Count; i += chunkSize)
                {
                    var chunk = aggregates.Skip(i).Take(chunkSize).ToList();
                    await context.AggregatedEnergies.AddRangeAsync(chunk);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
