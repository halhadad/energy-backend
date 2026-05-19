using energy_backend.Application.Models.SignalR;
using energy_backend.Application.Services;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class RealTimeDataQueryService : IRealTimeDataQueryService
    {
        private readonly EnergyDbContext _context;
        private readonly ILogger<RealTimeDataQueryService> _logger;

        public RealTimeDataQueryService(EnergyDbContext context, ILogger<RealTimeDataQueryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<RealTimeChartBucketDto>> GetAggregateMinuteEnergyForOrgAsync(
            Guid orgId, DateTime from, DateTime to)
        {
            return await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .GroupBy(a => a.Timestamp)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AveragePowerWatts),   // sum across devices = org total
                    MinWatts = g.Min(x => x.MinPowerWatts),
                    MaxWatts = g.Max(x => x.MaxPowerWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .OrderBy(b => b.Timestamp)
                .ToListAsync();
        }

        public async Task<List<RealTimeChartBucketDto>> GetAggregateHourEnergyForOrgAsync(
            Guid orgId, DateTime from, DateTime to)
        {
            return await _context.AggregateHourEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .GroupBy(a => a.Timestamp)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AveragePowerWatts),
                    MinWatts = g.Min(x => x.MinPowerWatts),
                    MaxWatts = g.Max(x => x.MaxPowerWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .OrderBy(b => b.Timestamp)
                .ToListAsync();
        }

        /// <summary>New method — day-level catch-up for day-range chart subscribers.</summary>
        public async Task<List<RealTimeChartBucketDto>> GetAggregateDayEnergyForOrgAsync(
            Guid orgId, DateTime from, DateTime to)
        {
            return await _context.AggregateDayEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .GroupBy(a => a.Timestamp)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AveragePowerWatts),
                    MinWatts = g.Min(x => x.MinPowerWatts),
                    MaxWatts = g.Max(x => x.MaxPowerWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .OrderBy(b => b.Timestamp)
                .ToListAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateMinuteEnergyForOrgAsync(Guid orgId)
        {
            return await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp)
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AveragePowerWatts),
                    MinWatts = g.Min(x => x.MinPowerWatts),
                    MaxWatts = g.Max(x => x.MaxPowerWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateHourEnergyForOrgAsync(Guid orgId)
        {
            return await _context.AggregateHourEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp)
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AveragePowerWatts),
                    MinWatts = g.Min(x => x.MinPowerWatts),
                    MaxWatts = g.Max(x => x.MaxPowerWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateDayEnergyForOrgAsync(Guid orgId)
        {
            return await _context.AggregateDayEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp)
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AveragePowerWatts),
                    MinWatts = g.Min(x => x.MinPowerWatts),
                    MaxWatts = g.Max(x => x.MaxPowerWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateMonthEnergyForOrgAsync(Guid orgId)
        {
            return await _context.AggregateMonthEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp)
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AveragePowerWatts),
                    MinWatts = g.Min(x => x.MinPowerWatts),
                    MaxWatts = g.Max(x => x.MaxPowerWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }
    }
}