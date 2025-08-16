using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Application.Models.SignalR;
using energy_backend.Application.Services;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Services
{
    public class RealTimeService(EnergyDbContext _context): IRealTimeService
    {
        public async Task<RealTimeDto> GetOverviewDataAsync(Guid organisationId)
        {
            var now = DateTime.UtcNow;
            var startOfToday = now.Date;
            var startOfWeek = now.Date.AddDays(-6);
            var fiveSecondsAgo = now.AddSeconds(-5);

            // Get readings
            var readingsToday = await _context.EnergyReadings
                .Include(r => r.Device)
                .Where(r => r.Device.OrganisationId == organisationId && r.Timestamp >= startOfToday)
                .ToListAsync();

            var readingsWeek = await _context.EnergyReadings
                .Include(r => r.Device)
                .Where(r => r.Device.OrganisationId == organisationId && r.Timestamp >= startOfWeek)
                .ToListAsync();

            var currentConsumption = await _context.EnergyReadings
                .Include(r => r.Device)
                .Where(r => r.Device.OrganisationId == organisationId && r.Timestamp >= fiveSecondsAgo)
                .SumAsync(r => r.EnergyValue);

            // Pie chart: sum by device type
            var pieDay = readingsToday
                .GroupBy(r => r.Device!.Type)
                .Select(g => new { Label = g.Key, Value = g.Sum(r => r.EnergyValue) })
                .ToList();

            var pieWeek = readingsWeek
                .GroupBy(r => r.Device!.Type)
                .Select(g => new { Label = g.Key, Value = g.Sum(r => r.EnergyValue) })
                .ToList();

            // Line chart: group by hour (day) or day (week)
            var lineDay = readingsToday
                .GroupBy(r => r.Timestamp.Hour)
                .OrderBy(g => g.Key)
                .Select(g => new { Label = $"{g.Key}:00", Value = g.Sum(r => r.EnergyValue) })
                .ToList();

            var lineWeek = readingsWeek
                .GroupBy(r => r.Timestamp.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Label = g.Key.ToString("ddd"), Value = g.Sum(r => r.EnergyValue) })
                .ToList();

            // Budget
            var organisation = await _context.Organisations.FindAsync(organisationId);
            var energyBudget = organisation?.EnergyBudget ?? 100f;

            return new RealTimeDto
            {
                PieChartDay = new BreakdownDto
                {
                    Labels = pieDay.Select(x => x.Label).ToList(),
                    Data = pieDay.Select(x => x.Value).ToList()
                },
                PieChartWeek = new BreakdownDto
                {
                    Labels = pieWeek.Select(x => x.Label).ToList(),
                    Data = pieWeek.Select(x => x.Value).ToList()
                },
                LineChartDay = new TimeSeriesDto
                {
                    Labels = lineDay.Select(x => x.Label).ToList(),
                    Data = lineDay.Select(x => x.Value).ToList()
                },
                LineChartWeek = new TimeSeriesDto
                {
                    Labels = lineWeek.Select(x => x.Label).ToList(),
                    Data = lineWeek.Select(x => x.Value).ToList()
                },
                Stats = new StatsDto
                {
                    CurrentConsumption = (float)Math.Round(currentConsumption, 2),
                    TodaysCost = (float)Math.Round(readingsToday.Sum(x => x.EnergyValue * 1.92), 2),
                    MonthlyBudget = energyBudget,
                    CarbonFootprint = (float)Math.Round(currentConsumption * 0.42f, 2)
                }
            };
        }

    }
}
