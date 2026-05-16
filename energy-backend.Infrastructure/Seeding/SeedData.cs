using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using energy_backend.Core.Entities;
using energy_backend.Data;
using energy_backend.Core.Entities;
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
                    
                        readings.Add(new EnergyReading
                        {
                            EnergyReadingId = Guid.NewGuid(),
                            DeviceId = device.DeviceId,
                            EnergyValue = (float)Math.Round(rng.NextDouble() * 0.01, 5),
                            Timestamp = currentTime
                        });
                    
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
            var devices = await context.Devices.Include(d => d.Organisation).AsNoTracking().ToListAsync();
            var startDate = DateTime.UtcNow.AddDays(-7).Date;
            var endDate = DateTime.UtcNow;
            var rng = new Random();

            foreach (var device in devices)
            {
                var orgId = device.OrganisationId;

                // 1. Seed Minute Aggregates
                var lastMinute = await context.AggregateMinuteEnergies
                    .Where(a => a.DeviceId == device.DeviceId)
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => a.Timestamp)
                    .FirstOrDefaultAsync();
                
                var minuteTime = lastMinute != default ? lastMinute.AddMinutes(1) : startDate;
                var minuteAggregates = new List<AggregateMinuteEnergy>();
                
                while (minuteTime <= endDate)
                {
                    minuteAggregates.Add(new AggregateMinuteEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId = orgId,
                        DeviceId = device.DeviceId,
                        Timestamp = minuteTime,
                        TotalEnergy = (float)Math.Round(rng.NextDouble() * 0.05, 4),
                        AverageWatts = (float)rng.Next(50, 500),
                        MinWatts = 40,
                        MaxWatts = 600,
                        DataPointsCount = 12
                    });
                    minuteTime = minuteTime.AddMinutes(1);
                    if (minuteAggregates.Count >= 2000)
                    {
                        await context.AggregateMinuteEnergies.AddRangeAsync(minuteAggregates);
                        await context.SaveChangesAsync();
                        minuteAggregates.Clear();
                    }
                }
                if (minuteAggregates.Count > 0)
                {
                    await context.AggregateMinuteEnergies.AddRangeAsync(minuteAggregates);
                    await context.SaveChangesAsync();
                }

                // 2. Seed Hour Aggregates
                var lastHour = await context.AggregateHourEnergies
                    .Where(a => a.DeviceId == device.DeviceId)
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => a.Timestamp)
                    .FirstOrDefaultAsync();

                var hourTime = lastHour != default ? lastHour.AddHours(1) : startDate;
                var hourAggregates = new List<AggregateHourEnergy>();
                
                while (hourTime <= endDate)
                {
                    hourAggregates.Add(new AggregateHourEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId = orgId,
                        DeviceId = device.DeviceId,
                        Timestamp = hourTime,
                        TotalEnergy = (float)Math.Round(rng.NextDouble() * 3, 4),
                        AverageWatts = (float)rng.Next(50, 500),
                        MinWatts = 40,
                        MaxWatts = 600,
                        DataPointsCount = 720
                    });
                    hourTime = hourTime.AddHours(1);
                }
                if (hourAggregates.Count > 0)
                {
                    await context.AggregateHourEnergies.AddRangeAsync(hourAggregates);
                    await context.SaveChangesAsync();
                }

                // 3. Seed Day Aggregates
                var lastDay = await context.AggregateDayEnergies
                    .Where(a => a.DeviceId == device.DeviceId)
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => a.Timestamp)
                    .FirstOrDefaultAsync();

                var dayTime = lastDay != default ? lastDay.AddDays(1) : startDate;
                var dayAggregates = new List<AggregateDayEnergy>();
                
                while (dayTime <= endDate)
                {
                    dayAggregates.Add(new AggregateDayEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId = orgId,
                        DeviceId = device.DeviceId,
                        Timestamp = dayTime,
                        TotalEnergy = (float)Math.Round(rng.NextDouble() * 50, 4),
                        AverageWatts = (float)rng.Next(50, 500),
                        MinWatts = 40,
                        MaxWatts = 600,
                        DataPointsCount = 17280
                    });
                    dayTime = dayTime.AddDays(1);
                }
                if (dayAggregates.Count > 0)
                {
                    await context.AggregateDayEnergies.AddRangeAsync(dayAggregates);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
