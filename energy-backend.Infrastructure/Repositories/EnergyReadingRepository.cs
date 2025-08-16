using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using energy_backend.Entities;
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
                .Where(r => r.Device.OrganisationId == organisationId && r.Timestamp >= startOfToday)
                .ToListAsync();
        }

        public async Task<List<EnergyReading>> GetReadingsForWeekAsync(Guid organisationId)
        {
            var startOfWeek = DateTime.UtcNow.Date.AddDays(-6);
            return await context.EnergyReadings
                .Include(r => r.Device)
                .Where(r => r.Device.OrganisationId == organisationId && r.Timestamp >= startOfWeek)
                .ToListAsync();
        }

        public async Task<float> GetCurrentConsumptionAsync(Guid organisationId, DateTime since)
        {
            return await context.EnergyReadings
                .Include(r => r.Device)
                .Where(r => r.Device.OrganisationId == organisationId && r.Timestamp >= since)
                .SumAsync(r => r.EnergyValue);
        }

        public async Task<float> GetTodaysCostAsync(Guid organisationId, float costPerUnit)
        {
            var readingsToday = await GetReadingsForTodayAsync(organisationId);
            return (float)Math.Round(readingsToday.Sum(x => x.EnergyValue * costPerUnit), 2);
        }

        public async Task<Dictionary<string, float>> GetDailyBreakdownByDeviceTypeAsync(Guid organisationId)
        {
            var readingsToday = await GetReadingsForTodayAsync(organisationId);
            return readingsToday
                .GroupBy(r => r.Device!.Type)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.EnergyValue));
        }

        public async Task<Dictionary<string, float>> GetWeeklyBreakdownByDeviceTypeAsync(Guid organisationId)
        {
            var readingsWeek = await GetReadingsForWeekAsync(organisationId);
            return readingsWeek
                .GroupBy(r => r.Device!.Type)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.EnergyValue));
        }

        public async Task<Dictionary<string, float>> GetHourlyBreakdownTodayAsync(Guid organisationId)
        {
            var readingsToday = await GetReadingsForTodayAsync(organisationId);
            return readingsToday
                .GroupBy(r => r.Timestamp.Hour)
                .OrderBy(g => g.Key)
                .ToDictionary(g => $"{g.Key}:00", g => g.Sum(r => r.EnergyValue));
        }

        public async Task<Dictionary<string, float>> GetDailyBreakdownThisWeekAsync(Guid organisationId)
        {
            var readingsWeek = await GetReadingsForWeekAsync(organisationId);
            return readingsWeek
                .GroupBy(r => r.Timestamp.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString("ddd"), g => g.Sum(r => r.EnergyValue));
        }
    }
}
