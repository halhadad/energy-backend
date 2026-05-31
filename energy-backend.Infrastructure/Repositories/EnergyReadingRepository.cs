using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using energy_backend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories
{
    public class EnergyReadingRepository(EnergyDbContext context) : IEnergyReadingRepository
    {
        public async Task<List<EnergyReading>> GetReadingsForTodayAsync(Guid organisationId)
        {
            var startOfToday = DateTime.UtcNow.Date;
            return await context.EnergyReadings
                .Include(r => r.Device)
                .Where(r => r.OrgId == organisationId && r.Timestamp >= startOfToday)
                .ToListAsync();
        }

        public async Task<List<EnergyReading>> GetReadingsForWeekAsync(Guid organisationId)
        {
            var startOfWeek = DateTime.UtcNow.Date.AddDays(-6);
            return await context.EnergyReadings
                .Include(r => r.Device)
                .Where(r => r.OrgId == organisationId && r.Timestamp >= startOfWeek)
                .ToListAsync();
        }

        public async Task<float> GetCurrentConsumptionAsync(Guid organisationId, DateTime since)
        {
            return await context.EnergyReadings
                .Where(r => r.OrgId == organisationId && r.Timestamp >= since)
                .SumAsync(r => r.ActivePowerWatts);
        }

        public async Task<float> GetTodaysCostAsync(Guid organisationId, float costPerUnit)
        {
            var readingsToday = await GetReadingsForTodayAsync(organisationId);
            return (float)Math.Round(readingsToday.Sum(x => x.ActivePowerWatts * costPerUnit), 2);
        }

        public async Task<Dictionary<string, float>> GetDailyBreakdownByDeviceTypeAsync(Guid organisationId)
        {
            var startOfToday = DateTime.UtcNow.Date;
            return await context.EnergyReadings
                .Where(r => r.OrgId == organisationId && r.Timestamp >= startOfToday)
                .GroupBy(r => r.Device!.Type)
                .ToDictionaryAsync(g => g.Key.ToString(), g => g.Sum(r => r.ActivePowerWatts));
        }

        public async Task<Dictionary<string, float>> GetWeeklyBreakdownByDeviceTypeAsync(Guid organisationId)
        {
            var startOfWeek = DateTime.UtcNow.Date.AddDays(-6);
            return await context.EnergyReadings
                .Where(r => r.OrgId == organisationId && r.Timestamp >= startOfWeek)
                .GroupBy(r => r.Device!.Type)
                .ToDictionaryAsync(g => g.Key.ToString(), g => g.Sum(r => r.ActivePowerWatts));
        }

        public async Task<Dictionary<string, float>> GetHourlyBreakdownTodayAsync(Guid organisationId)
        {
            var startOfToday = DateTime.UtcNow.Date;
            var rows = await context.EnergyReadings
                .Where(r => r.OrgId == organisationId && r.Timestamp >= startOfToday)
                .GroupBy(r => r.Timestamp.Hour)
                .Select(g => new { Hour = g.Key, Total = g.Sum(r => r.ActivePowerWatts) })
                .OrderBy(g => g.Hour)
                .ToListAsync();

            return rows.ToDictionary(g => $"{g.Hour}:00", g => g.Total);
        }

        public async Task<Dictionary<string, float>> GetDailyBreakdownThisWeekAsync(Guid organisationId)
        {
            var startOfWeek = DateTime.UtcNow.Date.AddDays(-6);
            var rows = await context.EnergyReadings
                .Where(r => r.OrgId == organisationId && r.Timestamp >= startOfWeek)
                .GroupBy(r => r.Timestamp.Date)
                .Select(g => new { Day = g.Key, Total = g.Sum(r => r.ActivePowerWatts) })
                .OrderBy(g => g.Day)
                .ToListAsync();

            return rows.ToDictionary(g => g.Day.ToString("ddd"), g => g.Total);
        }
    }
}
