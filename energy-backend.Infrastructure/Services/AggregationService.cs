using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class AggregationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AggregationService> _logger;

        public AggregationService(IServiceScopeFactory scopeFactory, ILogger<AggregationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                // run at 1 AM UTC every night
                if (now.Hour == 1)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();

                        var yesterday = DateTime.UtcNow.AddDays(-1).Date;

                        var endOfYesterday = yesterday.AddDays(1);

                        var rawData = await context.EnergyReadings
                            .Where(r => r.Timestamp >= yesterday && r.Timestamp < endOfYesterday)
                            .ToListAsync(stoppingToken);

                        var hourlyGroups = rawData
                            .GroupBy(r => new { r.DeviceId, Hour = new DateTime(r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day, r.Timestamp.Hour, 0, 0) })
                            .Select(g => new AggregatedEnergy
                            {
                                Id = Guid.NewGuid(),
                                DeviceId = g.Key.DeviceId,
                                PeriodStartTime = g.Key.Hour,
                                TotalKwh = g.Sum(x => x.EnergyValue)
                            })
                            .ToList();

                        await context.AggregatedEnergies.AddRangeAsync(hourlyGroups, stoppingToken);
                        await context.SaveChangesAsync(stoppingToken);

                        // clean up raw data older than 7 days
                        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                        var toDelete = context.EnergyReadings.Where(r => r.Timestamp < sevenDaysAgo);
                        context.EnergyReadings.RemoveRange(toDelete);
                        await context.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Aggregated yesterday's data successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during daily aggregation");
                    }

                    // sleep until the next day
                    await Task.Delay(TimeSpan.FromHours(23), stoppingToken);
                }
                else
                {
                    // check again in 1 hour if it's not yet time
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }
    }

}
