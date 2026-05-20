using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class HistoricalAggregationService : BackgroundService, IHistoricalAggregationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HistoricalAggregationService> _logger;

        public HistoricalAggregationService(
            IServiceScopeFactory scopeFactory,
            ILogger<HistoricalAggregationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Historical Aggregation Service starting.");
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var nextHour = now.AddHours(1).Date.AddHours(now.Hour + 1).AddMinutes(1);
                var delay = nextHour - now;
                if (delay.TotalMilliseconds < 0) delay = TimeSpan.FromSeconds(5);

                try { await Task.Delay(delay, stoppingToken); }
                catch (TaskCanceledException) { break; }

                if (stoppingToken.IsCancellationRequested) break;
                await RunHistoricalAggregationAsync(stoppingToken);
            }
        }

        public async Task RunHistoricalAggregationAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running historical aggregation at {Time}", DateTime.UtcNow);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();

                var currentHour = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);
                var processUpTo = currentHour;

                // ── Minute → Hour ─────────────────────────────────────────────
                var minuteGroups = await context.AggregateMinuteEnergies
                    .Where(m => m.Timestamp < processUpTo)
                    .GroupBy(m => new
                    {
                        m.OrgId,
                        m.DeviceId,
                        Hour = new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day, m.Timestamp.Hour, 0, 0)
                    })
                    .Select(g => new
                    {
                        g.Key.OrgId,
                        g.Key.DeviceId,
                        Timestamp = g.Key.Hour,
                        TotalEnergyKwh = g.Sum(x => x.TotalEnergyKwh),
                        AverageActivePowerWatts = g.Average(x => x.AverageActivePowerWatts),
                        MinActivePowerWatts = g.Min(x => x.MinActivePowerWatts),
                        MaxActivePowerWatts = g.Max(x => x.MaxActivePowerWatts),
                        AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                        AverageCurrentAmps = g.Average(x => x.AverageCurrentAmps),
                        AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                        DataPointsCount = g.Sum(x => x.DataPointsCount)
                    })
                    .ToListAsync(stoppingToken);

                foreach (var g in minuteGroups)
                {
                    var existing = await context.AggregateHourEnergies
                        .FirstOrDefaultAsync(h =>
                            h.OrgId == g.OrgId && h.DeviceId == g.DeviceId && h.Timestamp == g.Timestamp,
                            stoppingToken);

                    if (existing == null)
                    {
                        context.AggregateHourEnergies.Add(new AggregateHourEnergy
                        {
                            Id = Guid.NewGuid(),
                            OrgId = g.OrgId,
                            DeviceId = g.DeviceId,
                            Timestamp = g.Timestamp,
                            TotalEnergyKwh = g.TotalEnergyKwh,
                            AverageActivePowerWatts = g.AverageActivePowerWatts,
                            MinActivePowerWatts = g.MinActivePowerWatts,
                            MaxActivePowerWatts = g.MaxActivePowerWatts,
                            AverageVoltageVolts = g.AverageVoltageVolts,
                            AverageCurrentAmps = g.AverageCurrentAmps,
                            AveragePowerFactor = g.AveragePowerFactor,
                            DataPointsCount = g.DataPointsCount
                        });
                    }
                    else
                    {
                        existing.TotalEnergyKwh = g.TotalEnergyKwh;
                        existing.AverageActivePowerWatts = g.AverageActivePowerWatts;
                        existing.MinActivePowerWatts = g.MinActivePowerWatts;
                        existing.MaxActivePowerWatts = g.MaxActivePowerWatts;
                        existing.AverageVoltageVolts = g.AverageVoltageVolts;
                        existing.AverageCurrentAmps = g.AverageCurrentAmps;
                        existing.AveragePowerFactor = g.AveragePowerFactor;
                        existing.DataPointsCount = g.DataPointsCount;
                    }
                }
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Aggregated minute→hour up to {T}", processUpTo);

                // ── Hour → Day ────────────────────────────────────────────────
                var hourGroups = await context.AggregateHourEnergies
                    .Where(h => h.Timestamp < processUpTo.Date &&
                                !context.AggregateDayEnergies.Any(d =>
                                    d.OrgId == h.OrgId && d.DeviceId == h.DeviceId &&
                                    d.Timestamp == h.Timestamp.Date))
                    .GroupBy(h => new { h.OrgId, h.DeviceId, Day = h.Timestamp.Date })
                    .Select(g => new
                    {
                        g.Key.OrgId,
                        g.Key.DeviceId,
                        Timestamp = g.Key.Day,
                        TotalEnergyKwh = g.Sum(x => x.TotalEnergyKwh),
                        AverageActivePowerWatts = g.Average(x => x.AverageActivePowerWatts),
                        MinActivePowerWatts = g.Min(x => x.MinActivePowerWatts),
                        MaxActivePowerWatts = g.Max(x => x.MaxActivePowerWatts),
                        AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                        AverageCurrentAmps = g.Average(x => x.AverageCurrentAmps),
                        AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                        DataPointsCount = g.Sum(x => x.DataPointsCount)
                    })
                    .ToListAsync(stoppingToken);

                foreach (var g in hourGroups)
                {
                    context.AggregateDayEnergies.Add(new AggregateDayEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId = g.OrgId,
                        DeviceId = g.DeviceId,
                        Timestamp = g.Timestamp,
                        TotalEnergyKwh = g.TotalEnergyKwh,
                        AverageActivePowerWatts = g.AverageActivePowerWatts,
                        MinActivePowerWatts = g.MinActivePowerWatts,
                        MaxActivePowerWatts = g.MaxActivePowerWatts,
                        AverageVoltageVolts = g.AverageVoltageVolts,
                        AverageCurrentAmps = g.AverageCurrentAmps,
                        AveragePowerFactor = g.AveragePowerFactor,
                        DataPointsCount = g.DataPointsCount
                    });
                }
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Aggregated hour→day up to {T}", processUpTo.Date);

                // ── Day → Month ───────────────────────────────────────────────
                var currentDay = DateTime.UtcNow.Date;
                var dayGroups = await context.AggregateDayEnergies
                    .Where(d => d.Timestamp < currentDay &&
                                !context.AggregateMonthEnergies.Any(m =>
                                    m.OrgId == d.OrgId && m.DeviceId == d.DeviceId &&
                                    m.Timestamp.Year == d.Timestamp.Year &&
                                    m.Timestamp.Month == d.Timestamp.Month))
                    .GroupBy(d => new
                    {
                        d.OrgId,
                        d.DeviceId,
                        Month = new DateTime(d.Timestamp.Year, d.Timestamp.Month, 1)
                    })
                    .Select(g => new
                    {
                        g.Key.OrgId,
                        g.Key.DeviceId,
                        Timestamp = g.Key.Month,
                        TotalEnergyKwh = g.Sum(x => x.TotalEnergyKwh),
                        AverageActivePowerWatts = g.Average(x => x.AverageActivePowerWatts),
                        MinActivePowerWatts = g.Min(x => x.MinActivePowerWatts),
                        MaxActivePowerWatts = g.Max(x => x.MaxActivePowerWatts),
                        AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                        AverageCurrentAmps = g.Average(x => x.AverageCurrentAmps),
                        AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                        DataPointsCount = g.Sum(x => x.DataPointsCount)
                    })
                    .ToListAsync(stoppingToken);

                foreach (var g in dayGroups)
                {
                    context.AggregateMonthEnergies.Add(new AggregateMonthEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId = g.OrgId,
                        DeviceId = g.DeviceId,
                        Timestamp = g.Timestamp,
                        TotalEnergyKwh = g.TotalEnergyKwh,
                        AverageActivePowerWatts = g.AverageActivePowerWatts,
                        MinActivePowerWatts = g.MinActivePowerWatts,
                        MaxActivePowerWatts = g.MaxActivePowerWatts,
                        AverageVoltageVolts = g.AverageVoltageVolts,
                        AverageCurrentAmps = g.AverageCurrentAmps,
                        AveragePowerFactor = g.AveragePowerFactor,
                        DataPointsCount = g.DataPointsCount
                    });
                }
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Aggregated day→month up to {T}", currentDay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during historical aggregation");
            }
        }
    }
}