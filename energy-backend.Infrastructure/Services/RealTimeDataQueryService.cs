using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using energy_backend.Application.Services;
using energy_backend.Application.Models.SignalR; // For RealTimeChartBucketDto
using energy_backend.Core.Entities;
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

        public async Task<List<RealTimeChartBucketDto>> GetAggregateMinuteEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to)
        {
            return await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .GroupBy(a => a.Timestamp) // Aggregate per-device to org-level
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergy),
                    AverageWatts = g.Average(x => x.AverageWatts),
                    MinWatts = g.Min(x => x.MinWatts),
                    MaxWatts = g.Max(x => x.MaxWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .OrderBy(b => b.Timestamp)
                .ToListAsync();
        }

        public async Task<List<RealTimeChartBucketDto>> GetAggregateHourEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to)
        {
            return await _context.AggregateHourEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .GroupBy(a => a.Timestamp) // Aggregate per-device to org-level
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergy),
                    AverageWatts = g.Average(x => x.AverageWatts),
                    MinWatts = g.Min(x => x.MinWatts),
                    MaxWatts = g.Max(x => x.MaxWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .OrderBy(b => b.Timestamp)
                .ToListAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateMinuteEnergyForOrgAsync(Guid orgId)
        {
            return await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp) // Aggregate per-device to org-level
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergy),
                    AverageWatts = g.Average(x => x.AverageWatts),
                    MinWatts = g.Min(x => x.MinWatts),
                    MaxWatts = g.Max(x => x.MaxWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateHourEnergyForOrgAsync(Guid orgId)
        {
            return await _context.AggregateHourEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp) // Aggregate per-device to org-level
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergy),
                    AverageWatts = g.Average(x => x.AverageWatts),
                    MinWatts = g.Min(x => x.MinWatts),
                    MaxWatts = g.Max(x => x.MaxWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateDayEnergyForOrgAsync(Guid orgId)
        {
            return await _context.AggregateDayEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp) // Aggregate per-device to org-level
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergy),
                    AverageWatts = g.Average(x => x.AverageWatts),
                    MinWatts = g.Min(x => x.MinWatts),
                    MaxWatts = g.Max(x => x.MaxWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateMonthEnergyForOrgAsync(Guid orgId)
        {
            return await _context.AggregateMonthEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp) // Aggregate per-device to org-level
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergy),
                    AverageWatts = g.Average(x => x.AverageWatts),
                    MinWatts = g.Min(x => x.MinWatts),
                    MaxWatts = g.Max(x => x.MaxWatts),
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

    }
}
