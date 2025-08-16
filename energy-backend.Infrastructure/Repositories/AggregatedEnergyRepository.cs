using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories
{
    public class AggregatedEnergyRepository : IAggregatedEnergyRepository
    {
        private readonly EnergyDbContext _context;

    public AggregatedEnergyRepository(EnergyDbContext context)
    {
        _context = context;
    }
        public async Task<Dictionary<string, float>> GetAggregatedByDeviceTypeAsync(Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregatedEnergies
                .Include(a => a.Device)
                .Where(a => a.Device.OrganisationId == organisationId && a.PeriodStartTime >= start && a.PeriodStartTime < end)
                .GroupBy(a => a.Device.Type)
                .Select(g => new { g.Key, Total = g.Sum(x => x.TotalKwh) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);
        }

        public async Task<Dictionary<string, float>> GetAggregatedByHourAsync(Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregatedEnergies
                .Where(a => a.Device.OrganisationId == organisationId && a.PeriodStartTime >= start && a.PeriodStartTime < end)
                .GroupBy(a => a.PeriodStartTime.Hour)
                .OrderBy(g => g.Key)
                .Select(g => new { Hour = g.Key, Total = g.Sum(x => x.TotalKwh) })
                .ToDictionaryAsync(x => $"{x.Hour}:00", x => x.Total);
        }

        public async Task<Dictionary<string, float>> GetAggregatedByDayAsync(Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregatedEnergies
                .Where(a => a.Device.OrganisationId == organisationId && a.PeriodStartTime >= start && a.PeriodStartTime < end)
                .GroupBy(a => a.PeriodStartTime.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => x.TotalKwh) })
                .ToDictionaryAsync(x => x.Day.ToString("yyyy-MM-dd"), x => x.Total);
        }

        public async Task<float> GetTotalConsumptionAsync(Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregatedEnergies
                .Where(a => a.Device.OrganisationId == organisationId && a.PeriodStartTime >= start && a.PeriodStartTime < end)
                .SumAsync(a => a.TotalKwh);
        }
    }
}
