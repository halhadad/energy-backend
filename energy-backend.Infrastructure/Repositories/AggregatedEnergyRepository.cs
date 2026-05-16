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
            // For device type breakdown, we use the minute aggregates for the highest precision in the requested range
            return await _context.AggregateMinuteEnergies
                .Include(a => a.Device)
                .Where(a => a.OrgId == organisationId && a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(a => a.Device.Type)
                .Select(g => new { g.Key, Total = g.Sum(x => x.TotalEnergy) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);
        }

        public async Task<Dictionary<string, float>> GetAggregatedByHourAsync(Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregateHourEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(a => a.Timestamp) // Group by timestamp to sum across all devices for that org
                .OrderBy(g => g.Key)
                .Select(g => new { Hour = g.Key.Hour, Total = g.Sum(x => x.TotalEnergy) })
                .ToDictionaryAsync(x => $"{x.Hour}:00", x => x.Total);
        }

        public async Task<Dictionary<string, float>> GetAggregatedByDayAsync(Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregateDayEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(a => a.Timestamp.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => x.TotalEnergy) })
                .ToDictionaryAsync(x => x.Day.ToString("yyyy-MM-dd"), x => x.Total);
        }

        public async Task<float> GetTotalConsumptionAsync(Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp >= start && a.Timestamp < end)
                .SumAsync(a => a.TotalEnergy);
        }
    }
}
