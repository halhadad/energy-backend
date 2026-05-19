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

        /// <summary>
        /// Returns kWh totalled by device type for a time range.
        /// Used for pie/breakdown charts.
        /// </summary>
        public async Task<Dictionary<string, float>> GetAggregatedByDeviceTypeAsync(
            Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregateMinuteEnergies
                .Include(a => a.Device)
                .Where(a => a.OrgId == organisationId && a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(a => a.Device!.Type)
                .Select(g => new { g.Key, Total = g.Sum(x => x.TotalEnergyKwh) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);
        }

        /// <summary>
        /// Returns kWh per hour-of-day for a time range.
        /// Used for the day consumption line chart.
        /// </summary>
        public async Task<Dictionary<string, float>> GetAggregatedByHourAsync(
            Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregateHourEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(a => a.Timestamp)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Hour = g.Key.Hour,
                    Total = g.Sum(x => x.TotalEnergyKwh)
                })
                .ToDictionaryAsync(x => $"{x.Hour}:00", x => x.Total);
        }

        /// <summary>
        /// Returns kWh per calendar day for a time range.
        /// Used for the week/month consumption line chart.
        /// </summary>
        public async Task<Dictionary<string, float>> GetAggregatedByDayAsync(
            Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregateDayEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp >= start && a.Timestamp < end)
                .GroupBy(a => a.Timestamp.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Day = g.Key,
                    Total = g.Sum(x => x.TotalEnergyKwh)
                })
                .ToDictionaryAsync(x => x.Day.ToString("yyyy-MM-dd"), x => x.Total);
        }

        /// <summary>
        /// Returns total kWh consumed across all devices in a time range.
        /// Used for month-to-date consumption total.
        /// </summary>
        public async Task<float> GetTotalConsumptionAsync(
            Guid organisationId, DateTime start, DateTime end)
        {
            return await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == organisationId && a.Timestamp >= start && a.Timestamp < end)
                .SumAsync(a => a.TotalEnergyKwh);
        }
    }
}