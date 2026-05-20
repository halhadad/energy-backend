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
    /// Seeds 7 days of historical aggregate data using realistic watt values
    /// derived from each device's RatedPowerWatts.
    ///
    /// Run on every startup — skips any time ranges already populated so it
    /// is safe to call repeatedly without duplicating data.
    /// </summary>
    public static class SeedData
    {
        public static async Task SeedAggregatedEnergyDbAsync(EnergyDbContext context)
        {
            var devices = await context.Devices
                .Include(d => d.Organisation)
                .AsNoTracking()
                .ToListAsync();

            if (!devices.Any()) return;

            // Fixed seed for reproducible history across restarts
            var rng = new Random(42);
            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);

            foreach (var device in devices)
            {
                var ratedW = device.RatedPowerWatts > 0
                    ? device.RatedPowerWatts
                    : (float)(rng.NextDouble() * 450.0 + 50.0);

                await SeedMinutesAsync(context, device, ratedW, sevenDaysAgo, now, rng);
                await SeedHoursAsync(context, device, ratedW, sevenDaysAgo, now, rng);
                await SeedDaysAsync(context, device, ratedW, sevenDaysAgo, now, rng);
                await SeedMonthAsync(context, device, ratedW, now, rng);
            }
        }

        // ── Minute buckets ────────────────────────────────────────────────────

        private static async Task SeedMinutesAsync(EnergyDbContext ctx, Device device,
            float ratedW, DateTime from, DateTime to, Random rng)
        {
            var lastTs = await ctx.AggregateMinuteEnergies
                .Where(a => a.DeviceId == device.DeviceId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            var cursor = lastTs.HasValue
                ? lastTs.Value.AddMinutes(1)
                : FloorMinute(from);

            var batch = new List<AggregateMinuteEnergy>(2000);

            while (cursor < to)
            {
                var watts = SimulateWatts(ratedW, cursor, rng);
                // kWh for one minute = W × 60s / 3,600,000
                var kwh = watts * 60f / 3_600_000f;

                batch.Add(new AggregateMinuteEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = device.OrganisationId,
                    DeviceId = device.DeviceId,
                    Timestamp = cursor,
                    AverageActivePowerWatts = watts,
                    MinActivePowerWatts = watts * 0.88f,
                    MaxActivePowerWatts = watts * 1.12f,
                    TotalEnergyKwh = kwh,
                    DataPointsCount = 12,
                    AverageVoltageVolts = 230f,
                    AverageCurrentAmps = watts / 230f,
                    AveragePowerFactor = 0.95f
                });

                cursor = cursor.AddMinutes(1);

                if (batch.Count >= 2000)
                {
                    await ctx.AggregateMinuteEnergies.AddRangeAsync(batch);
                    await ctx.SaveChangesAsync();
                    batch.Clear();
                }
            }

            if (batch.Any())
            {
                await ctx.AggregateMinuteEnergies.AddRangeAsync(batch);
                await ctx.SaveChangesAsync();
            }
        }

        // ── Hour buckets ──────────────────────────────────────────────────────

        private static async Task SeedHoursAsync(EnergyDbContext ctx, Device device,
            float ratedW, DateTime from, DateTime to, Random rng)
        {
            var lastTs = await ctx.AggregateHourEnergies
                .Where(a => a.DeviceId == device.DeviceId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            var cursor = lastTs.HasValue
                ? lastTs.Value.AddHours(1)
                : FloorHour(from);

            var batch = new List<AggregateHourEnergy>();

            while (cursor < to)
            {
                var watts = SimulateWatts(ratedW, cursor, rng);
                // kWh for one hour = W / 1000
                var kwh = watts / 1000f;

                batch.Add(new AggregateHourEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = device.OrganisationId,
                    DeviceId = device.DeviceId,
                    Timestamp = cursor,
                    AverageActivePowerWatts = watts,
                    MinActivePowerWatts = watts * 0.75f,
                    MaxActivePowerWatts = watts * 1.25f,
                    TotalEnergyKwh = kwh,
                    DataPointsCount = 720,
                    AverageVoltageVolts = 230f,
                    AverageCurrentAmps = watts / 230f,
                    AveragePowerFactor = 0.95f
                });

                cursor = cursor.AddHours(1);
            }

            if (batch.Any())
            {
                await ctx.AggregateHourEnergies.AddRangeAsync(batch);
                await ctx.SaveChangesAsync();
            }
        }

        // ── Day buckets ───────────────────────────────────────────────────────

        private static async Task SeedDaysAsync(EnergyDbContext ctx, Device device,
            float ratedW, DateTime from, DateTime to, Random rng)
        {
            var lastTs = await ctx.AggregateDayEnergies
                .Where(a => a.DeviceId == device.DeviceId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            var cursor = lastTs.HasValue
                ? lastTs.Value.AddDays(1)
                : from.Date;

            var batch = new List<AggregateDayEnergy>();

            while (cursor.Date < to.Date)
            {
                var watts = SimulateWatts(ratedW, cursor, rng);
                // kWh for one day = W × 24h / 1000
                var kwh = watts * 24f / 1000f;

                batch.Add(new AggregateDayEnergy
                {
                    Id = Guid.NewGuid(),
                    OrgId = device.OrganisationId,
                    DeviceId = device.DeviceId,
                    Timestamp = cursor.Date,
                    AverageActivePowerWatts = watts,
                    MinActivePowerWatts = watts * 0.60f,
                    MaxActivePowerWatts = watts * 1.40f,
                    TotalEnergyKwh = kwh,
                    DataPointsCount = 17280,
                    AverageVoltageVolts = 230f,
                    AverageCurrentAmps = watts / 230f,
                    AveragePowerFactor = 0.95f
                });

                cursor = cursor.AddDays(1);
            }

            if (batch.Any())
            {
                await ctx.AggregateDayEnergies.AddRangeAsync(batch);
                await ctx.SaveChangesAsync();
            }
        }

        // ── Month bucket ──────────────────────────────────────────────────────

        private static async Task SeedMonthAsync(EnergyDbContext ctx, Device device,
            float ratedW, DateTime now, Random rng)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var exists = await ctx.AggregateMonthEnergies
                .AnyAsync(a => a.DeviceId == device.DeviceId && a.Timestamp == monthStart);

            if (exists) return;

            var watts = SimulateWatts(ratedW, monthStart.AddHours(12), rng); // midday reference
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            var kwh = watts * 24f * daysInMonth / 1000f;

            await ctx.AggregateMonthEnergies.AddAsync(new AggregateMonthEnergy
            {
                Id = Guid.NewGuid(),
                OrgId = device.OrganisationId,
                DeviceId = device.DeviceId,
                Timestamp = monthStart,
                AverageActivePowerWatts = watts,
                MinActivePowerWatts = watts * 0.55f,
                MaxActivePowerWatts = watts * 1.45f,
                TotalEnergyKwh = kwh,
                DataPointsCount = daysInMonth * 17280,
                AverageVoltageVolts = 230f,
                AverageCurrentAmps = watts / 230f,
                AveragePowerFactor = 0.95f
            });

            await ctx.SaveChangesAsync();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a realistic watt value for a device at the given UTC time.
        /// Anchored to ratedW with time-of-day load factor and ±25% noise.
        /// Matches the load curve used by EnergyReadingSimulator so seeded
        /// history is consistent with live readings.
        /// </summary>
        private static float SimulateWatts(float ratedW, DateTime utcTime, Random rng)
        {
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

            var noise = (rng.NextDouble() * 0.50) - 0.25;
            return (float)Math.Max(0.0, ratedW * loadFactor * (1.0 + noise));
        }

        private static DateTime FloorMinute(DateTime dt) =>
            new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);

        private static DateTime FloorHour(DateTime dt) =>
            new(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc);
    }
}