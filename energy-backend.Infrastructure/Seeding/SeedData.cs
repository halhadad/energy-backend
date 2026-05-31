using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using energy_backend.Core.Entities;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Seeding
{
    /// <summary>
    /// Seeds 3 months of historical aggregate data using realistic watt values
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
            var threeMonthsAgo = now.AddMonths(-3);

            foreach (var device in devices)
            {
                await SeedDeviceHistoryAsync(context, device, threeMonthsAgo, now, rng);
            }
        }

        public static async Task SeedDeviceHistoryAsync(EnergyDbContext context, Guid deviceId)
        {
            var device = await context.Devices
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

            if (device == null) return;

            await SeedDeviceHistoryAsync(context, device, DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow, new Random(42));
        }

        private static async Task SeedDeviceHistoryAsync(
            EnergyDbContext context,
            Device device,
            DateTime from,
            DateTime to,
            Random rng)
        {
            // FIX: Normalize and floor raw inputs to clean, stable minute boundaries immediately.
            // This prevents moving millisecond sliding windows from shifting your data ranges on subsequent calls.
            var cleanFrom = new DateTime(from.Year, from.Month, from.Day, from.Hour, from.Minute, 0, DateTimeKind.Utc);
            var cleanTo = new DateTime(to.Year, to.Month, to.Day, to.Hour, to.Minute, 0, DateTimeKind.Utc);

            var ratedW = device.RatedPowerWatts > 0
                ? device.RatedPowerWatts
                : (float)(rng.NextDouble() * 450.0 + 50.0);

            // Pass fully normalized boundaries down to all seed workers
            await SeedMinutesAsync(context, device, ratedW, cleanFrom, cleanTo, rng);
            await SeedHoursAsync(context, device, ratedW, cleanFrom, cleanTo, rng);
            await SeedDaysAsync(context, device, ratedW, cleanFrom, cleanTo, rng);
            await SeedMonthsAsync(context, device, ratedW, cleanFrom, cleanTo, rng);
        }

        // ── Minute buckets ────────────────────────────────────────────────────

        private static async Task SeedMinutesAsync(EnergyDbContext ctx, Device device,
            float ratedW, DateTime from, DateTime to, Random rng)
        {
            var start = FloorMinute(from);
            var end = FloorMinute(to);

            var existing = await ctx.AggregateMinuteEnergies
                .AsNoTracking()
                .Where(a => a.DeviceId == device.DeviceId)
                .Where(a => a.Timestamp >= start && a.Timestamp <= end)
                .Select(a => a.Timestamp)
                .ToHashSetAsync();

            var cursor = start;
            var batch = new List<AggregateMinuteEnergy>(2000);

            // FIX: Evaluate using '<= end' instead of '< to' to prevent hanging un-truncated seconds from pushing an extra row.
            while (cursor <= end)
            {
                if (existing.Contains(cursor))
                {
                    cursor = cursor.AddMinutes(1);
                    continue;
                }

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
                    TotalActiveEnergyKwh = kwh,
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
            var start = FloorHour(from);
            var end = FloorHour(to);

            var existing = await ctx.AggregateHourEnergies
                .AsNoTracking()
                .Where(a => a.DeviceId == device.DeviceId)
                .Where(a => a.Timestamp >= start && a.Timestamp <= end)
                .Select(a => a.Timestamp)
                .ToHashSetAsync();

            var cursor = start;
            var batch = new List<AggregateHourEnergy>();

            // FIX: Evaluate using '<= end' instead of '< to'
            while (cursor <= end)
            {
                if (existing.Contains(cursor))
                {
                    cursor = cursor.AddHours(1);
                    continue;
                }

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
                    TotalActiveEnergyKwh = kwh,
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
            var start = from.Date;
            var end = to.Date;

            var existing = await ctx.AggregateDayEnergies
                .AsNoTracking()
                .Where(a => a.DeviceId == device.DeviceId)
                .Where(a => a.Timestamp >= start && a.Timestamp <= end)
                .Select(a => a.Timestamp)
                .ToHashSetAsync();

            var cursor = start;
            var batch = new List<AggregateDayEnergy>();

            // FIX: Aligned evaluation for full days
            while (cursor.Date <= end)
            {
                if (existing.Contains(cursor.Date))
                {
                    cursor = cursor.AddDays(1);
                    continue;
                }

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
                    TotalActiveEnergyKwh = kwh,
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

        private static async Task SeedMonthsAsync(EnergyDbContext ctx, Device device,
            float ratedW, DateTime from, DateTime to, Random rng)
        {
            var cursor = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endMonth = new DateTime(to.Year, to.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var existing = await ctx.AggregateMonthEnergies
                .AsNoTracking()
                .Where(a => a.DeviceId == device.DeviceId)
                .Where(a => a.Timestamp >= cursor && a.Timestamp <= endMonth)
                .Select(a => a.Timestamp)
                .ToHashSetAsync();

            while (cursor <= endMonth)
            {
                if (!existing.Contains(cursor))
                {
                    var watts = SimulateWatts(ratedW, cursor.AddHours(12), rng); // midday reference
                    var daysInMonth = DateTime.DaysInMonth(cursor.Year, cursor.Month);
                    var kwh = watts * 24f * daysInMonth / 1000f;

                    await ctx.AggregateMonthEnergies.AddAsync(new AggregateMonthEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId = device.OrganisationId,
                        DeviceId = device.DeviceId,
                        Timestamp = cursor,
                        AverageActivePowerWatts = watts,
                        MinActivePowerWatts = watts * 0.55f,
                        MaxActivePowerWatts = watts * 1.45f,
                        TotalActiveEnergyKwh = kwh,
                        DataPointsCount = daysInMonth * 17280,
                        AverageVoltageVolts = 230f,
                        AverageCurrentAmps = watts / 230f,
                        AveragePowerFactor = 0.95f
                    });
                }

                cursor = cursor.AddMonths(1);
            }

            await ctx.SaveChangesAsync();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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