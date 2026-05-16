using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Application.Services; // Added for IHistoricalAggregationService
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

        public HistoricalAggregationService(IServiceScopeFactory scopeFactory, ILogger<HistoricalAggregationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run every hour, starting 1 minute past the hour to ensure previous hour's data is complete
            _logger.LogInformation("Historical Aggregation Service starting.");
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var nextHour = now.AddHours(1).Date.AddHours(now.Hour + 1).AddMinutes(1); // 1 minute past next hour
                var delay = nextHour - now;

                if (delay.TotalMilliseconds < 0) // Already past the next trigger, run immediately then adjust for next cycle
                {
                    delay = TimeSpan.FromSeconds(5); // Small delay to avoid busy loop
                }
                
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

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

                // Get the last completed hour (e.g., if now is 10:35, process 09:00-09:59)
                var currentHour = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);
                var processUpTo = currentHour; // Process up to the beginning of the current hour

                // Aggregate Minute -> Hour
                // Find minute aggregates that haven't been processed into hour aggregates yet
                var unprocessedMinuteAggregates = await context.AggregateMinuteEnergies
                    .Where(m => m.Timestamp < processUpTo)
                    .GroupBy(m => new { m.OrgId, m.DeviceId, Hour = new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day, m.Timestamp.Hour, 0, 0) }) // Group by DeviceId as well
                    .Select(g => new
                    {
                        OrgId = g.Key.OrgId,
                        DeviceId = g.Key.DeviceId, // Select DeviceId
                        Timestamp = g.Key.Hour,
                        TotalEnergy = g.Sum(x => x.TotalEnergy),
                        AverageWatts = g.Average(x => x.AverageWatts),
                        MinWatts = g.Min(x => x.MinWatts),
                        MaxWatts = g.Max(x => x.MaxWatts),
                        DataPointsCount = g.Sum(x => x.DataPointsCount)
                    })
                    .ToListAsync(stoppingToken);

                foreach (var group in unprocessedMinuteAggregates)
                {
                    var existingHourAggregate = await context.AggregateHourEnergies
                        .FirstOrDefaultAsync(h => h.OrgId == group.OrgId && h.DeviceId == group.DeviceId && h.Timestamp == group.Timestamp, stoppingToken); // Query by DeviceId

                    if (existingHourAggregate == null)
                    {
                        context.AggregateHourEnergies.Add(new AggregateHourEnergy
                        {
                            Id = Guid.NewGuid(),
                            OrgId = group.OrgId,
                            DeviceId = group.DeviceId, // Set DeviceId
                            Timestamp = group.Timestamp,
                            TotalEnergy = group.TotalEnergy,
                            AverageWatts = group.AverageWatts,
                            MinWatts = group.MinWatts,
                            MaxWatts = group.MaxWatts,
                            DataPointsCount = group.DataPointsCount
                        });
                    }
                    else
                    {
                        // Update existing (e.g., in case of late data arrival or recalculation)
                        existingHourAggregate.TotalEnergy = group.TotalEnergy;
                        existingHourAggregate.AverageWatts = group.AverageWatts;
                        existingHourAggregate.MinWatts = group.MinWatts;
                        existingHourAggregate.MaxWatts = group.MaxWatts;
                        existingHourAggregate.DataPointsCount = group.DataPointsCount;
                    }
                }
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Aggregated minute to hour aggregates up to {ProcessUpTo}", processUpTo);

                // Aggregate Hour -> Day
                var unprocessedHourAggregates = await context.AggregateHourEnergies
                    .Where(h => h.Timestamp < processUpTo.Date && // Only process full days
                                !context.AggregateDayEnergies.Any(d => d.OrgId == h.OrgId && d.DeviceId == h.DeviceId && d.Timestamp == h.Timestamp.Date)) // Exclude already processed, query by DeviceId
                    .GroupBy(h => new { h.OrgId, h.DeviceId, Day = h.Timestamp.Date }) // Group by DeviceId as well
                    .Select(g => new
                    {
                        OrgId = g.Key.OrgId,
                        DeviceId = g.Key.DeviceId, // Select DeviceId
                        Timestamp = g.Key.Day,
                        TotalEnergy = g.Sum(x => x.TotalEnergy),
                        AverageWatts = g.Average(x => x.AverageWatts),
                        MinWatts = g.Min(x => x.MinWatts),
                        MaxWatts = g.Max(x => x.MaxWatts),
                        DataPointsCount = g.Sum(x => x.DataPointsCount)
                    })
                    .ToListAsync(stoppingToken);

                foreach (var group in unprocessedHourAggregates)
                {
                    context.AggregateDayEnergies.Add(new AggregateDayEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId = group.OrgId,
                        DeviceId = group.DeviceId, // Set DeviceId
                        Timestamp = group.Timestamp,
                        TotalEnergy = group.TotalEnergy,
                        AverageWatts = group.AverageWatts,
                        MinWatts = group.MinWatts,
                        MaxWatts = group.MaxWatts,
                        DataPointsCount = group.DataPointsCount
                    });
                }
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Aggregated hour to day aggregates up to {ProcessUpTo}", processUpTo.Date);

                // Aggregate Day -> Month (Run less frequently, e.g., daily for full previous days)
                var currentDay = DateTime.UtcNow.Date;
                var unprocessedDayAggregates = await context.AggregateDayEnergies
                    .Where(d => d.Timestamp < currentDay.Date && // Only process full months
                                !context.AggregateMonthEnergies.Any(m => m.OrgId == d.OrgId && m.DeviceId == d.DeviceId && m.Timestamp.Year == d.Timestamp.Year && m.Timestamp.Month == d.Timestamp.Month)) // Exclude already processed, query by DeviceId
                    .GroupBy(d => new { d.OrgId, d.DeviceId, Month = new DateTime(d.Timestamp.Year, d.Timestamp.Month, 1) }) // Group by DeviceId as well
                    .Select(g => new
                    {
                        OrgId = g.Key.OrgId,
                        DeviceId = g.Key.DeviceId, // Select DeviceId
                        Timestamp = g.Key.Month,
                        TotalEnergy = g.Sum(x => x.TotalEnergy),
                        AverageWatts = g.Average(x => x.AverageWatts),
                        MinWatts = g.Min(x => x.MinWatts),
                        MaxWatts = g.Max(x => x.MaxWatts),
                        DataPointsCount = g.Sum(x => x.DataPointsCount)
                    })
                    .ToListAsync(stoppingToken);

                foreach (var group in unprocessedDayAggregates)
                {
                    context.AggregateMonthEnergies.Add(new AggregateMonthEnergy
                    {
                        Id = Guid.NewGuid(),
                        OrgId = group.OrgId,
                        DeviceId = group.DeviceId, // Set DeviceId
                        Timestamp = group.Timestamp,
                        TotalEnergy = group.TotalEnergy,
                        AverageWatts = group.AverageWatts,
                        MinWatts = group.MinWatts,
                        MaxWatts = group.MaxWatts,
                        DataPointsCount = group.DataPointsCount
                    });
                }
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Aggregated day to month aggregates up to {ProcessUpTo}", currentDay.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during historical aggregation");
            }
        }
    }
}
