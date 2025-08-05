using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var devices = await context.Devices.ToListAsync();
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var rng = new Random();

            foreach (var device in devices)
            {
                var currentTime = startDate;
                var readings = new List<EnergyReading>();

                while (currentTime <= endDate)
                {
                    readings.Add(new EnergyReading
                    {
                        EnergyReadingId = Guid.NewGuid(),
                        DeviceId = device.DeviceId,
                        EnergyValue = (float)Math.Round(rng.NextDouble() * 0.01, 5), // 0–0.01 kWh
                        Timestamp = currentTime
                    });

                    currentTime = currentTime.AddSeconds(5);
                }

                // chunk insert
                const int chunkSize = 5000;
                for (int i = 0; i < readings.Count; i += chunkSize)
                {
                    var chunk = readings.Skip(i).Take(chunkSize);
                    await context.EnergyReadings.AddRangeAsync(chunk);
                    await context.SaveChangesAsync();
                }
            }
        }

        public static async Task SeedAggregatedEnergyDbAsync(EnergyDbContext context)
        {
            var devices = await context.Devices.ToListAsync();
            var startDate = DateTime.UtcNow.AddDays(-7).Date;
            var endDate = DateTime.UtcNow;
            var rng = new Random();

            foreach (var device in devices)
            {
                var currentTime = startDate;
                var aggregates = new List<AggregatedEnergy>();

                while (currentTime <= endDate)
                {
                    aggregates.Add(new AggregatedEnergy
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = device.DeviceId,
                        PeriodStartTime = currentTime,
                        TotalKwh = (float)Math.Round((decimal)(rng.NextDouble() * 3), 4)
                    });

                    currentTime = currentTime.AddHours(1);
                }

                // chunk insert
                const int chunkSize = 1000;
                for (int i = 0; i < aggregates.Count; i += chunkSize)
                {
                    var chunk = aggregates.Skip(i).Take(chunkSize);
                    await context.AggregatedEnergies.AddRangeAsync(chunk);
                    await context.SaveChangesAsync();
                }
            }
        }
    }

}
