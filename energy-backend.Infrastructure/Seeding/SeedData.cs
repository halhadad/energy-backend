using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Seeding
{
    /// <summary>
    /// Seeds historical aggregate tables with realistic data.
    ///
    /// Data model: EnergyReading.EnergyValue = instantaneous Watts.
    /// Aggregate buckets store:
    ///   AverageWatts — mean power over the window (what the dashboard shows)
    ///   TotalEnergy  — kWh consumed in the window (for cost/carbon charts)
    ///
    /// Seeding skips devices that already have data, so it is safe to run on
    /// every startup without duplicating records.
    /// </summary>
    public static class SeedData
    {
        private const float CostPerKwh = 1.92f;

        public static async Task SeedAggregatedEnergyDbAsync(EnergyDbContext context)
        {
            var devices = await context.Devices
                .Include(d => d.Organisation)
                .AsNoTracking()
                .ToListAsync();

            if (!devices.Any()) return;

            var rng = new Random(42); // Fixed seed for reproducible data
            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);

            foreach (var device in devices)
            {
                // Use the device's rated power as the simulation baseline.
                // If not set, pick a sensible random default and keep it stable
                // for this device throughout the seed run.
                var ratedW = device.EnergyConsumption > 0
                    ? device.EnergyConsumption
                    : (float)(rng.NextDouble() * 480.0 + 20.0);

                await SeedMinutesAsync(context, device, ratedW, sevenDaysAgo, now, rng);
                await SeedHoursAsync(context, device, ratedW, sevenDaysAgo, now, rng);
                await SeedDaysAsync(context, device, ratedW, sevenDaysAgo, now, rng);
                await SeedMonthAsync(context, device, ratedW, now, rng);
            }
        }

        // ── Minute aggregates (last 7 days, one row per minute per device) ───

        private static async Task SeedMinutesAsync(
            EnergyDbContext context, Device device, float ratedW,
            DateTime from, DateTime to, Random rng)
        {
            // Find where we left off so we don't re-seed
            var lastTs = await context.AggregateMinuteEnergies
                .Where(a => a.DeviceId == device.DeviceId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            var cursor = lastTs.HasValue
                ? lastTs.Value.AddMinutes(1)
                : FloorToMinute(from);

            var batch = new List<AggregateMinuteEnergy>();

            while (cursor < to)
            {
                var watts = SimulateWatts(ratedW, cursor, rng);
                // kWh for one minute = W × (60s / 3,600,000)
                var kwhPerMinute = watts * 60f / 3_600_000f;

                batch.Add(new AggregateMinuteEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = device.OrganisationId,
                    DeviceId = device.DeviceId,
                    Timestamp = cursor,
                    AverageWatts = watts,
                    MinWatts = watts * 0.90f,
                    MaxWatts = watts * 1.10f,
                    TotalEnergy = kwhPerMinute,
                    DataPointsCount = 12 // 12 × 5-second readings per minute
                });

                cursor = cursor.AddMinutes(1);

                if (batch.Count >= 2000)
                {
                    await context.AggregateMinuteEnergies.AddRangeAsync(batch);
                    await context.SaveChangesAsync();
                    batch.Clear();
                }
            }

            if (batch.Any())
            {
                await context.AggregateMinuteEnergies.AddRangeAsync(batch);
                await context.SaveChangesAsync();
            }
        }

        // ── Hour aggregates ──────────────────────────────────────────────────

        private static async Task SeedHoursAsync(
            EnergyDbContext context, Device device, float ratedW,
            DateTime from, DateTime to, Random rng)
        {
            var lastTs = await context.AggregateHourEnergies
                .Where(a => a.DeviceId == device.DeviceId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            var cursor = lastTs.HasValue
                ? lastTs.Value.AddHours(1)
                : FloorToHour(from);

            var batch = new List<AggregateHourEnergy>();

            while (cursor < to)
            {
                var watts = SimulateWatts(ratedW, cursor, rng);
                // kWh for one hour = W / 1000
                var kwhPerHour = watts / 1000f;

                batch.Add(new AggregateHourEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = device.OrganisationId,
                    DeviceId = device.DeviceId,
                    Timestamp = cursor,
                    AverageWatts = watts,
                    MinWatts = watts * 0.85f,
                    MaxWatts = watts * 1.15f,
                    TotalEnergy = kwhPerHour,
                    DataPointsCount = 720 // 720 × 5-second readings per hour
                });

                cursor = cursor.AddHours(1);
            }

            if (batch.Any())
            {
                await context.AggregateHourEnergies.AddRangeAsync(batch);
                await context.SaveChangesAsync();
            }
        }

        // ── Day aggregates ───────────────────────────────────────────────────

        private static async Task SeedDaysAsync(
            EnergyDbContext context, Device device, float ratedW,
            DateTime from, DateTime to, Random rng)
        {
            var lastTs = await context.AggregateDayEnergies
                .Where(a => a.DeviceId == device.DeviceId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            var cursor = lastTs.HasValue
                ? lastTs.Value.AddDays(1)
                : from.Date;

            var batch = new List<AggregateDayEnergy>();

            while (cursor.Date < to.Date)
            {
                var watts = SimulateWatts(ratedW, cursor, rng);
                // kWh for one day = W × 24 / 1000
                var kwhPerDay = watts * 24f / 1000f;

                batch.Add(new AggregateDayEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = device.OrganisationId,
                    DeviceId = device.DeviceId,
                    Timestamp = cursor.Date,
                    AverageWatts = watts,
                    MinWatts = watts * 0.70f,
                    MaxWatts = watts * 1.30f,
                    TotalEnergy = kwhPerDay,
                    DataPointsCount = 17280 // 17280 × 5-second readings per day
                });

                cursor = cursor.AddDays(1);
            }

            if (batch.Any())
            {
                await context.AggregateDayEnergies.AddRangeAsync(batch);
                await context.SaveChangesAsync();
            }
        }

        // ── Month aggregate ──────────────────────────────────────────────────

        private static async Task SeedMonthAsync(
            EnergyDbContext context, Device device, float ratedW,
            DateTime now, Random rng)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var exists = await context.AggregateMonthEnergies
                .AnyAsync(a => a.DeviceId == device.DeviceId && a.Timestamp == monthStart);

            if (exists) return;

            var watts = SimulateWatts(ratedW, monthStart, rng);
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            var kwhPerMonth = watts * 24f * daysInMonth / 1000f;

            await context.AggregateMonthEnergies.AddAsync(new AggregateMonthEnergy
            {
                Id = Guid.NewGuid(),
                OrgId = device.OrganisationId,
                DeviceId = device.DeviceId,
                Timestamp = monthStart,
                AverageWatts = watts,
                MinWatts = watts * 0.60f,
                MaxWatts = watts * 1.40f,
                TotalEnergy = kwhPerMonth,
                DataPointsCount = daysInMonth * 17280
            });

            await context.SaveChangesAsync();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a realistic watt value based on time of day.
        /// Morning/evening peaks, low overnight — matches real household/office patterns.
        /// Adds ±15% random jitter.
        /// </summary>
        private static float SimulateWatts(float ratedW, DateTime ts, Random rng)
        {
            // Time-of-day load factor: 0.3 overnight, peak 1.0 at 8am and 6pm
            var hour = ts.Hour;
            double loadFactor = hour switch
            {
                >= 0 and < 6 => 0.25,                    // Night — minimal
                >= 6 and < 9 => 0.6 + (hour - 6) * 0.1, // Morning ramp-up
                >= 9 and < 17 => 0.75,                   // Business hours
                >= 17 and < 20 => 0.90,                  // Evening peak
                >= 20 and < 23 => 0.65,                  // Wind-down
                _ => 0.30                                 // Late night
            };

            var jitter = (rng.NextDouble() * 0.30) - 0.15; // ±15%
            var watts = (float)(ratedW * loadFactor * (1.0 + jitter));
            return Math.Max(0f, watts);
        }

        private static DateTime FloorToMinute(DateTime dt) =>
            new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);

        private static DateTime FloorToHour(DateTime dt) =>
            new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc);
    }
}