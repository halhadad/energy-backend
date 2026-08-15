using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Seeding
{
    // seeds 3 months of aggregate history per device. higher tiers are derived from the
    // seeded minutes so every tier agrees, and already-seeded ranges are skipped
    public static class SeedData
    {
        public static async Task SeedAggregatedEnergyDbAsync(EnergyDbContext context, decimal defaultRatePerKwh)
        {
            var devices = await context.Devices
                .Include(d => d.Organisation)
                .AsNoTracking()
                .ToListAsync();

            if (!devices.Any()) return;

            var rng = new Random(42);
            var now = DateTime.UtcNow;
            var threeMonthsAgo = now.AddMonths(-3);

            foreach (var device in devices)
                await SeedDeviceHistoryAsync(context, device, threeMonthsAgo, now, rng);

            await SeedRatesAsync(context, devices.Select(d => d.OrganisationId).Distinct(), defaultRatePerKwh);
        }

        private static async Task SeedRatesAsync(
            EnergyDbContext ctx, IEnumerable<Guid> orgIds, decimal defaultRatePerKwh)
        {
            foreach (var orgId in orgIds)
            {
                if (await ctx.EnergyRates.AnyAsync(r => r.OrganisationId == orgId)) continue;
                await ctx.EnergyRates.AddAsync(new EnergyRate
                {
                    Id = Guid.NewGuid(),
                    OrganisationId = orgId,
                    RatePerKwh = defaultRatePerKwh,
                    ValidFromUtc = DateTime.UtcNow.AddMonths(-3),
                    ValidToUtc = null
                });
            }
            await ctx.SaveChangesAsync();
        }

        public static async Task SeedDeviceHistoryAsync(EnergyDbContext context, Guid deviceId)
        {
            var device = await context.Devices.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId);
            if (device is null) return;
            await SeedDeviceHistoryAsync(context, device, DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow, new Random(42));
        }

        private static async Task SeedDeviceHistoryAsync(
            EnergyDbContext context, Device device, DateTime from, DateTime to, Random rng)
        {
            var cleanFrom = FloorMinute(from);
            var cleanTo   = FloorMinute(to);

            var ratedW = device.RatedPowerWatts > 0
                ? device.RatedPowerWatts
                : rng.NextDouble() * 450.0 + 50.0;

            // Seed the finest tier first; derive coarser tiers from it so all tiers agree.
            await SeedMinutesAsync(context, device, ratedW, cleanFrom, cleanTo, rng);
            await SeedHoursFromMinutesAsync(context, device, cleanFrom, cleanTo);
            await SeedDaysFromHoursAsync(context, device, cleanFrom, cleanTo);
            await SeedMonthsFromDaysAsync(context, device, cleanFrom, cleanTo);
        }

        // minute seeding
        private static async Task SeedMinutesAsync(EnergyDbContext ctx, Device device,
            double ratedW, DateTime from, DateTime to, Random rng)
        {
            var start = FloorMinute(from);
            var end   = FloorMinute(to);

            var existing = await ctx.AggregateMinuteEnergies.AsNoTracking()
                .Where(a => a.DeviceId == device.DeviceId && a.Timestamp >= start && a.Timestamp <= end)
                .Select(a => a.Timestamp)
                .ToHashSetAsync();

            var cursor = start;
            var batch  = new List<AggregateMinuteEnergy>(2000);

            while (cursor <= end)
            {
                if (!existing.Contains(cursor))
                {
                    var watts = SimulateWatts(ratedW, cursor, rng);
                    var kwh   = watts * 60d / 3_600_000d;

                    batch.Add(new AggregateMinuteEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId    = device.OrganisationId,
                        DeviceId = device.DeviceId,
                        Timestamp = cursor,
                        TotalActiveEnergyKwh    = kwh,
                        AverageActivePowerWatts = watts,
                        MinActivePowerWatts     = watts,
                        MaxActivePowerWatts     = watts,
                        DataPointsCount         = 12,
                        AverageVoltageVolts     = 230d,
                        AverageCurrentAmps      = watts / 230d,
                        AveragePowerFactor      = 0.95
                    });

                    if (batch.Count >= 2000)
                    {
                        await ctx.AggregateMinuteEnergies.AddRangeAsync(batch);
                        await ctx.SaveChangesAsync();
                        batch.Clear();
                    }
                }

                cursor = cursor.AddMinutes(1);
            }

            if (batch.Any())
            {
                await ctx.AggregateMinuteEnergies.AddRangeAsync(batch);
                await ctx.SaveChangesAsync();
            }
        }

        // higher tiers derived from the tier below
        private static async Task SeedHoursFromMinutesAsync(
            EnergyDbContext ctx, Device device, DateTime from, DateTime to)
        {
            var start = FloorHour(from);
            var end   = FloorHour(to);

            var existingHours = await ctx.AggregateHourEnergies.AsNoTracking()
                .Where(h => h.DeviceId == device.DeviceId && h.Timestamp >= start && h.Timestamp <= end)
                .Select(h => h.Timestamp)
                .ToHashSetAsync();

            var minutes = await ctx.AggregateMinuteEnergies.AsNoTracking()
                .Where(m => m.DeviceId == device.DeviceId && m.Timestamp >= start && m.Timestamp < end.AddHours(1))
                .ToListAsync();

            if (minutes.Count == 0) return;

            var batch = new List<AggregateHourEnergy>();

            foreach (var g in minutes.GroupBy(m => FloorHour(m.Timestamp)))
            {
                if (existingHours.Contains(g.Key)) continue;
                batch.Add(RollupTo<AggregateHourEnergy>(device.OrganisationId, device.DeviceId, g.Key, g));
            }

            if (batch.Any())
            {
                await ctx.AggregateHourEnergies.AddRangeAsync(batch);
                await ctx.SaveChangesAsync();
            }
        }

        private static async Task SeedDaysFromHoursAsync(
            EnergyDbContext ctx, Device device, DateTime from, DateTime to)
        {
            var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
            var end   = DateTime.SpecifyKind(to.Date,   DateTimeKind.Utc);

            var existingDays = await ctx.AggregateDayEnergies.AsNoTracking()
                .Where(d => d.DeviceId == device.DeviceId && d.Timestamp >= start && d.Timestamp <= end)
                .Select(d => d.Timestamp)
                .ToHashSetAsync();

            var hours = await ctx.AggregateHourEnergies.AsNoTracking()
                .Where(h => h.DeviceId == device.DeviceId && h.Timestamp >= start && h.Timestamp < end.AddDays(1))
                .ToListAsync();

            if (hours.Count == 0) return;

            var batch = new List<AggregateDayEnergy>();

            foreach (var g in hours.GroupBy(h => DateTime.SpecifyKind(h.Timestamp.Date, DateTimeKind.Utc)))
            {
                if (existingDays.Contains(g.Key)) continue;
                batch.Add(RollupTo<AggregateDayEnergy>(device.OrganisationId, device.DeviceId, g.Key, g));
            }

            if (batch.Any())
            {
                await ctx.AggregateDayEnergies.AddRangeAsync(batch);
                await ctx.SaveChangesAsync();
            }
        }

        private static async Task SeedMonthsFromDaysAsync(
            EnergyDbContext ctx, Device device, DateTime from, DateTime to)
        {
            var start = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end   = new DateTime(to.Year,   to.Month,   1, 0, 0, 0, DateTimeKind.Utc);

            var existingMonths = await ctx.AggregateMonthEnergies.AsNoTracking()
                .Where(m => m.DeviceId == device.DeviceId && m.Timestamp >= start && m.Timestamp <= end)
                .Select(m => m.Timestamp)
                .ToHashSetAsync();

            var days = await ctx.AggregateDayEnergies.AsNoTracking()
                .Where(d => d.DeviceId == device.DeviceId && d.Timestamp >= start && d.Timestamp < end.AddMonths(1))
                .ToListAsync();

            if (days.Count == 0) return;

            var batch = new List<AggregateMonthEnergy>();

            foreach (var g in days.GroupBy(d => new DateTime(d.Timestamp.Year, d.Timestamp.Month, 1, 0, 0, 0, DateTimeKind.Utc)))
            {
                if (existingMonths.Contains(g.Key)) continue;
                batch.Add(RollupTo<AggregateMonthEnergy>(device.OrganisationId, device.DeviceId, g.Key, g));
            }

            if (batch.Any())
            {
                await ctx.AggregateMonthEnergies.AddRangeAsync(batch);
                await ctx.SaveChangesAsync();
            }
        }

        // shared rollup helper, same logic as the downsampling worker
        private static T RollupTo<T>(
            Guid orgId, Guid deviceId, DateTime slot, IEnumerable<IEnergyAggregate> source)
            where T : IEnergyAggregate, new()
        {
            var list   = source.ToList();
            var points = list.Sum(x => x.DataPointsCount);

            double Weighted(Func<IEnergyAggregate, double> sel)
                => points > 0 ? list.Sum(x => sel(x) * x.DataPointsCount) / points : 0;

            return new T
            {
                Id       = Guid.NewGuid(),
                OrgId    = orgId,
                DeviceId = deviceId,
                Timestamp = slot,
                TotalActiveEnergyKwh    = list.Sum(x => x.TotalActiveEnergyKwh),
                AverageActivePowerWatts = Weighted(x => x.AverageActivePowerWatts),
                AverageVoltageVolts     = Weighted(x => x.AverageVoltageVolts),
                AverageCurrentAmps      = Weighted(x => x.AverageCurrentAmps),
                AveragePowerFactor      = Weighted(x => x.AveragePowerFactor),
                MinActivePowerWatts     = list.Min(x => x.MinActivePowerWatts),
                MaxActivePowerWatts     = list.Max(x => x.MaxActivePowerWatts),
                DataPointsCount         = points
            };
        }

        private static double SimulateWatts(double ratedW, DateTime utcTime, Random rng)
        {
            var hour = utcTime.Hour;
            double loadFactor = hour switch
            {
                >= 0 and < 5  => 0.15,
                >= 5 and < 7  => 0.30 + (hour - 5) * 0.15,
                >= 7 and < 9  => 0.65 + (hour - 7) * 0.10,
                >= 9 and < 12 => 0.85,
                >= 12 and < 14 => 0.75,
                >= 14 and < 17 => 0.85,
                >= 17 and < 20 => 1.00,
                >= 20 and < 22 => 0.80,
                _ => 0.50
            };

            var noise = (rng.NextDouble() * 0.50) - 0.25;
            return Math.Max(0.0, ratedW * loadFactor * (1.0 + noise));
        }

        private static DateTime FloorMinute(DateTime dt) =>
            new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);

        private static DateTime FloorHour(DateTime dt) =>
            new(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc);
    }
}
