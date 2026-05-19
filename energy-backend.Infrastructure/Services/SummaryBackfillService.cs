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
    public class SummaryBackfillService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SummaryBackfillService> _logger;

        public SummaryBackfillService(IServiceScopeFactory scopeFactory, ILogger<SummaryBackfillService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();

                db.Database.SetCommandTimeout(300); // 5 minutes timeout for heavy query

                var months = await db.EnergyReadings
                    .GroupBy(r => new { r.DeviceId, r.Timestamp.Year, r.Timestamp.Month })
                    .Select(g => new
                    {
                        g.Key.DeviceId,
                        g.Key.Year,
                        g.Key.Month,
                        Total = g.Sum(r => r.PowerWatts)
                    })
                    .ToListAsync(stoppingToken);

                foreach (var m in months)
                {
                    var summary = await db.DeviceConsumptionSummaries
                        .FirstOrDefaultAsync(s => s.DeviceId == m.DeviceId && s.Year == m.Year && s.Month == m.Month, stoppingToken);

                    if (summary == null)
                    {
                        summary = new DeviceConsumptionSummary
                        {
                            DeviceConsumptionSummaryId = Guid.NewGuid(),
                            DeviceId = m.DeviceId,
                            Year = m.Year,
                            Month = m.Month,
                            TotalConsumption = m.Total,
                            LastUpdated = new DateTime(m.Year, m.Month, DateTime.DaysInMonth(m.Year, m.Month))
                        };
                        db.DeviceConsumptionSummaries.Add(summary);
                    }
                    else
                    {
                        summary.TotalConsumption = m.Total;
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Backfilled summaries for {Count} device-months", months.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backfill summaries due to a timeout or error.");
            }
        }
    }

}
