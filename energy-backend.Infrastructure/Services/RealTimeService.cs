using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Application.Models.SignalR;
using energy_backend.Application.Services;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using energy_backend.Core.Entities; // Added for new aggregate entities

namespace energy_backend.Infrastructure.Services
{
    public class RealTimeService(EnergyDbContext _context): IRealTimeService
    {
        public async Task<RealTimeDto> GetOverviewDataAsync(Guid organisationId)
        {
            var now = DateTime.UtcNow;
            var startOfToday = now.Date;
            var startOfWeek = now.Date.AddDays(-6);
            var fiveSecondsAgo = now.AddSeconds(-5); // Still used for current consumption fallback

            // --- Pie Charts (Still relying on raw readings for device type grouping) ---
            var readingsTodayRaw = await _context.EnergyReadings
                .Include(r => r.Device)
                .Where(r => r.Device.OrganisationId == organisationId && r.Timestamp >= startOfToday)
                .ToListAsync();

            var readingsWeekRaw = await _context.EnergyReadings
                .Include(r => r.Device)
                .Where(r => r.Device.OrganisationId == organisationId && r.Timestamp >= startOfWeek)
                .ToListAsync();

            var pieDay = readingsTodayRaw
                .GroupBy(r => r.Device!.Type)
                .Select(g => new { Label = g.Key, Value = g.Sum(r => r.EnergyValue) })
                .ToList();

            var pieWeek = readingsWeekRaw
                .GroupBy(r => r.Device!.Type)
                .Select(g => new { Label = g.Key, Value = g.Sum(r => r.EnergyValue) })
                .ToList();

            // --- Current Consumption (Derived from latest minute aggregate or raw if aggregate not found) ---
            var currentMinuteStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
            var latestMinuteAggregate = await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp == currentMinuteStart)
                .FirstOrDefaultAsync();
            
            float currentConsumption = latestMinuteAggregate?.AverageWatts ?? 
                                       await _context.EnergyReadings // Fallback to raw if no aggregate
                                           .Where(r => r.Device!.OrganisationId == organisationId && r.Timestamp >= fiveSecondsAgo)
                                           .SumAsync(r => r.EnergyValue);

            // --- Line Charts (Using aggregate tables) ---
            // Line Chart Day: last 24 hours from AggregateHourEnergy
            var last24Hours = await _context.AggregateHourEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp >= now.AddHours(-24) && a.Timestamp < now)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            var lineDay = last24Hours
                .Select(a => new { Label = $"{a.Timestamp:HH:mm}", Value = a.TotalEnergy })
                .ToList();

            // Line Chart Week: last 7 days from AggregateDayEnergy
            var last7Days = await _context.AggregateDayEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp >= now.AddDays(-7).Date && a.Timestamp < now.Date)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            var lineWeek = last7Days
                .Select(a => new { Label = $"{a.Timestamp:ddd}", Value = a.TotalEnergy })
                .ToList();

            // --- Stats (Using aggregate tables) ---
            // TodaysCost: Sum of TotalEnergy from AggregateMinuteEnergy for today
            var todaysEnergy = await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp >= startOfToday && a.Timestamp < now)
                .SumAsync(a => a.TotalEnergy);
            float todaysCost = (float)Math.Round(todaysEnergy * 1.92, 2);

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
                    TodaysCost = todaysCost,
                    MonthlyBudget = energyBudget,
                    CarbonFootprint = (float)Math.Round(currentConsumption * 0.42f, 2)
                }
            };
        }

    }
}
