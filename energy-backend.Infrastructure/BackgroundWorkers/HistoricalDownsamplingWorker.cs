using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace energy_backend.Infrastructure.BackgroundWorkers;

// runs hourly and promotes minute buckets up to hour, then day, then month.
// minute buckets are written elsewhere (the simulator), this only rolls up completed periods
public class HistoricalDownsamplingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HistoricalDownsamplingWorker> _logger;
    private readonly int _lookbackHours;

    public HistoricalDownsamplingWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EnergyWorkerOptions> options,
        ILogger<HistoricalDownsamplingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _lookbackHours = options.Value.DownsamplerLookbackHours;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Historical Data Downsampling Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            // Floor to the current hour, then add one hour plus a small buffer.
            var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc)
                .AddHours(1)
                .AddMinutes(1);

            var delay = nextHour - now;
            if (delay.TotalMilliseconds < 0) delay = TimeSpan.FromSeconds(5);

            _logger.LogInformation(
                "Historical downsampler sleeping {Delay:hh\\:mm\\:ss} until {NextHour:HH:mm} UTC.",
                delay, nextHour);

            try { await Task.Delay(delay, stoppingToken); }
            catch (TaskCanceledException) { break; }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var aggregateRepo = scope.ServiceProvider.GetRequiredService<IAggregateRepository>();

                await RollupMinutesToHoursAsync(aggregateRepo, now, _lookbackHours, stoppingToken);
                await RollupHoursToDaysAsync(aggregateRepo, now, stoppingToken);
                await RollupDaysToMonthsAsync(aggregateRepo, now, stoppingToken);

                _logger.LogInformation("Historical rollup chain completed for hour {Hour}.", now.Hour);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error inside historical downsampling loop.");
            }
        }
    }

    private static async Task RollupMinutesToHoursAsync(
        IAggregateRepository repo, DateTime now, int lookbackHours, CancellationToken ct)
    {
        var hourBoundary = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var lookbackStart = hourBoundary.AddHours(-lookbackHours);
        var minutes = await repo.GetMinutesBeforeAsync(lookbackStart, hourBoundary, ct);

        var groups = minutes.GroupBy(m => new
        {
            m.OrgId,
            m.DeviceId,
            HourSlot = new DateTime(
                m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day,
                m.Timestamp.Hour, 0, 0, DateTimeKind.Utc)
        });

        foreach (var g in groups)
        {
            if (await repo.HourExistsAsync(g.Key.OrgId, g.Key.DeviceId, g.Key.HourSlot, ct)) continue;
            await repo.AddHourAsync(
                Rollup<AggregateHourEnergy>(g.Key.OrgId, g.Key.DeviceId, g.Key.HourSlot, g), ct);
        }

        await repo.SaveChangesAsync(ct);
    }

    private static async Task RollupHoursToDaysAsync(
        IAggregateRepository repo, DateTime now, CancellationToken ct)
    {
        var dayBoundary = now.Date;
        var hours = await repo.GetHoursBeforeAsync(dayBoundary, ct);

        var groups = hours.GroupBy(h => new
        {
            h.OrgId,
            h.DeviceId,
            DaySlot = h.Timestamp.Date
        });

        foreach (var g in groups)
        {
            if (await repo.DayExistsAsync(g.Key.OrgId, g.Key.DeviceId, g.Key.DaySlot, ct)) continue;
            await repo.AddDayAsync(
                Rollup<AggregateDayEnergy>(g.Key.OrgId, g.Key.DeviceId, g.Key.DaySlot, g), ct);
        }

        await repo.SaveChangesAsync(ct);
    }

    private static async Task RollupDaysToMonthsAsync(
        IAggregateRepository repo, DateTime now, CancellationToken ct)
    {
        var monthBoundary = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var days = await repo.GetDaysBeforeAsync(monthBoundary, ct);

        var groups = days.GroupBy(d => new
        {
            d.OrgId,
            d.DeviceId,
            MonthSlot = new DateTime(d.Timestamp.Year, d.Timestamp.Month, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        foreach (var g in groups)
        {
            if (await repo.MonthExistsAsync(g.Key.OrgId, g.Key.DeviceId, g.Key.MonthSlot, ct)) continue;
            await repo.AddMonthAsync(
                Rollup<AggregateMonthEnergy>(g.Key.OrgId, g.Key.DeviceId, g.Key.MonthSlot, g), ct);
        }

        await repo.SaveChangesAsync(ct);
    }

    private static T Rollup<T>(
        Guid orgId, Guid? deviceId, DateTime slot, IEnumerable<IEnergyAggregate> source)
        where T : IEnergyAggregate, new()
    {
        var list = source.ToList();
        var points = list.Sum(x => x.DataPointsCount);

        return new T
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            DeviceId = deviceId,
            Timestamp = slot,
            TotalActiveEnergyKwh = list.Sum(x => x.TotalActiveEnergyKwh),
            AverageActivePowerWatts = points > 0
                ? list.Sum(x => x.AverageActivePowerWatts * x.DataPointsCount) / points : 0,
            AverageVoltageVolts = points > 0
                ? list.Sum(x => x.AverageVoltageVolts * x.DataPointsCount) / points : 0,
            AverageCurrentAmps = points > 0
                ? list.Sum(x => x.AverageCurrentAmps * x.DataPointsCount) / points : 0,
            AveragePowerFactor = points > 0
                ? list.Sum(x => x.AveragePowerFactor * x.DataPointsCount) / points : 0,
            MinActivePowerWatts = list.Count > 0 ? list.Min(x => x.MinActivePowerWatts) : 0,
            MaxActivePowerWatts = list.Count > 0 ? list.Max(x => x.MaxActivePowerWatts) : 0,
            DataPointsCount = points
        };
    }
}