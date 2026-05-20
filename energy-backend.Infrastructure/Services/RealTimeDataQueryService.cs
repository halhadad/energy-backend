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

        // ── Org-level queries (summed across all devices) ─────────────────────

        public async Task<List<RealTimeChartBucketDto>> GetAggregateMinuteEnergyForOrgAsync(
            Guid orgId, DateTime from, DateTime to)
        {
            var costRate = await GetCostRateAsync(orgId);
            return await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .GroupBy(a => a.Timestamp)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AverageActivePowerWatts),
                    MinWatts = g.Min(x => x.MinActivePowerWatts),
                    MaxWatts = g.Max(x => x.MaxActivePowerWatts),
                    AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                    TotalCurrentAmps = g.Sum(x => x.AverageCurrentAmps),
                    AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                    EstimatedCost = g.Sum(x => x.TotalEnergyKwh) * costRate,
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .OrderBy(b => b.Timestamp)
                .ToListAsync();
        }

        public async Task<List<RealTimeChartBucketDto>> GetAggregateHourEnergyForOrgAsync(
            Guid orgId, DateTime from, DateTime to)
        {
            var costRate = await GetCostRateAsync(orgId);
            return await _context.AggregateHourEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .GroupBy(a => a.Timestamp)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AverageActivePowerWatts),
                    MinWatts = g.Min(x => x.MinActivePowerWatts),
                    MaxWatts = g.Max(x => x.MaxActivePowerWatts),
                    AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                    TotalCurrentAmps = g.Sum(x => x.AverageCurrentAmps),
                    AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                    EstimatedCost = g.Sum(x => x.TotalEnergyKwh) * costRate,
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .OrderBy(b => b.Timestamp)
                .ToListAsync();
        }

        public async Task<List<RealTimeChartBucketDto>> GetAggregateDayEnergyForOrgAsync(
            Guid orgId, DateTime from, DateTime to)
        {
            var costRate = await GetCostRateAsync(orgId);
            return await _context.AggregateDayEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .GroupBy(a => a.Timestamp)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AverageActivePowerWatts),
                    MinWatts = g.Min(x => x.MinActivePowerWatts),
                    MaxWatts = g.Max(x => x.MaxActivePowerWatts),
                    AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                    TotalCurrentAmps = g.Sum(x => x.AverageCurrentAmps),
                    AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                    EstimatedCost = g.Sum(x => x.TotalEnergyKwh) * costRate,
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .OrderBy(b => b.Timestamp)
                .ToListAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateMinuteEnergyForOrgAsync(Guid orgId)
        {
            var costRate = await GetCostRateAsync(orgId);
            return await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp)
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AverageActivePowerWatts),
                    MinWatts = g.Min(x => x.MinActivePowerWatts),
                    MaxWatts = g.Max(x => x.MaxActivePowerWatts),
                    AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                    TotalCurrentAmps = g.Sum(x => x.AverageCurrentAmps),
                    AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                    EstimatedCost = g.Sum(x => x.TotalEnergyKwh) * costRate,
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateHourEnergyForOrgAsync(Guid orgId)
        {
            var costRate = await GetCostRateAsync(orgId);
            return await _context.AggregateHourEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp)
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AverageActivePowerWatts),
                    MinWatts = g.Min(x => x.MinActivePowerWatts),
                    MaxWatts = g.Max(x => x.MaxActivePowerWatts),
                    AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                    TotalCurrentAmps = g.Sum(x => x.AverageCurrentAmps),
                    AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                    EstimatedCost = g.Sum(x => x.TotalEnergyKwh) * costRate,
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateDayEnergyForOrgAsync(Guid orgId)
        {
            var costRate = await GetCostRateAsync(orgId);
            return await _context.AggregateDayEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp)
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AverageActivePowerWatts),
                    MinWatts = g.Min(x => x.MinActivePowerWatts),
                    MaxWatts = g.Max(x => x.MaxActivePowerWatts),
                    AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                    TotalCurrentAmps = g.Sum(x => x.AverageCurrentAmps),
                    AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                    EstimatedCost = g.Sum(x => x.TotalEnergyKwh) * costRate,
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RealTimeChartBucketDto?> GetLatestAggregateMonthEnergyForOrgAsync(Guid orgId)
        {
            var costRate = await GetCostRateAsync(orgId);
            return await _context.AggregateMonthEnergies
                .Where(a => a.OrgId == orgId)
                .GroupBy(a => a.Timestamp)
                .OrderByDescending(g => g.Key)
                .Select(g => new RealTimeChartBucketDto
                {
                    Timestamp = g.Key,
                    TotalEnergy = g.Sum(x => x.TotalEnergyKwh),
                    AverageWatts = g.Sum(x => x.AverageActivePowerWatts),
                    MinWatts = g.Min(x => x.MinActivePowerWatts),
                    MaxWatts = g.Max(x => x.MaxActivePowerWatts),
                    AverageVoltageVolts = g.Average(x => x.AverageVoltageVolts),
                    TotalCurrentAmps = g.Sum(x => x.AverageCurrentAmps),
                    AveragePowerFactor = g.Average(x => x.AveragePowerFactor),
                    EstimatedCost = g.Sum(x => x.TotalEnergyKwh) * costRate,
                    DataPointsCount = g.Sum(x => x.DataPointsCount)
                })
                .FirstOrDefaultAsync();
        }

        // ── Per-device queries ────────────────────────────────────────────────

        public async Task<List<DeviceBucketsDto>> GetDeviceMinuteBucketsForOrgAsync(
            Guid orgId, DateTime from, DateTime to)
        {
            var costRate = await GetCostRateAsync(orgId);
            var rows = await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .Include(a => a.Device)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            return GroupToDeviceSeries(rows.Select(a => (
                a.DeviceId,
                a.Device?.Name ?? a.DeviceId.ToString(),
                a.Device?.Type ?? "",
                new DeviceBucketDto
                {
                    Timestamp = a.Timestamp,
                    TotalEnergy = a.TotalEnergyKwh,
                    AverageWatts = a.AverageActivePowerWatts,
                    AverageVoltageVolts = a.AverageVoltageVolts,
                    AverageCurrentAmps = a.AverageCurrentAmps,
                    AveragePowerFactor = a.AveragePowerFactor,
                    EstimatedCost = a.TotalEnergyKwh * costRate
                }
            )).ToList());
        }

        public async Task<List<DeviceBucketsDto>> GetDeviceHourBucketsForOrgAsync(
            Guid orgId, DateTime from, DateTime to)
        {
            var costRate = await GetCostRateAsync(orgId);
            var rows = await _context.AggregateHourEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .Include(a => a.Device)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            return GroupToDeviceSeries(rows.Select(a => (
                a.DeviceId,
                a.Device?.Name ?? a.DeviceId.ToString(),
                a.Device?.Type ?? "",
                new DeviceBucketDto
                {
                    Timestamp = a.Timestamp,
                    TotalEnergy = a.TotalEnergyKwh,
                    AverageWatts = a.AverageActivePowerWatts,
                    AverageVoltageVolts = a.AverageVoltageVolts,
                    AverageCurrentAmps = a.AverageCurrentAmps,
                    AveragePowerFactor = a.AveragePowerFactor,
                    EstimatedCost = a.TotalEnergyKwh * costRate
                }
            )).ToList());
        }

        public async Task<List<DeviceBucketsDto>> GetDeviceDayBucketsForOrgAsync(
            Guid orgId, DateTime from, DateTime to)
        {
            var costRate = await GetCostRateAsync(orgId);
            var rows = await _context.AggregateDayEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp >= from && a.Timestamp < to)
                .Include(a => a.Device)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            return GroupToDeviceSeries(rows.Select(a => (
                a.DeviceId,
                a.Device?.Name ?? a.DeviceId.ToString(),
                a.Device?.Type ?? "",
                new DeviceBucketDto
                {
                    Timestamp = a.Timestamp,
                    TotalEnergy = a.TotalEnergyKwh,
                    AverageWatts = a.AverageActivePowerWatts,
                    AverageVoltageVolts = a.AverageVoltageVolts,
                    AverageCurrentAmps = a.AverageCurrentAmps,
                    AveragePowerFactor = a.AveragePowerFactor,
                    EstimatedCost = a.TotalEnergyKwh * costRate
                }
            )).ToList());
        }

        public async Task<List<DeviceBucketsDto>> GetLatestDeviceMinuteBucketsForOrgAsync(Guid orgId)
        {
            var costRate = await GetCostRateAsync(orgId);
            var latestTs = await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == orgId)
                .MaxAsync(a => (DateTime?)a.Timestamp);

            if (latestTs == null) return new List<DeviceBucketsDto>();

            var rows = await _context.AggregateMinuteEnergies
                .Where(a => a.OrgId == orgId && a.Timestamp == latestTs.Value)
                .Include(a => a.Device)
                .ToListAsync();

            return GroupToDeviceSeries(rows.Select(a => (
                a.DeviceId,
                a.Device?.Name ?? a.DeviceId.ToString(),
                a.Device?.Type ?? "",
                new DeviceBucketDto
                {
                    Timestamp = a.Timestamp,
                    TotalEnergy = a.TotalEnergyKwh,
                    AverageWatts = a.AverageActivePowerWatts,
                    AverageVoltageVolts = a.AverageVoltageVolts,
                    AverageCurrentAmps = a.AverageCurrentAmps,
                    AveragePowerFactor = a.AveragePowerFactor,
                    EstimatedCost = a.TotalEnergyKwh * costRate
                }
            )).ToList());
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the electricity cost rate for the org's owner.
        /// Falls back to 0.28 if no user setting exists.
        /// </summary>
        private async Task<float> GetCostRateAsync(Guid orgId)
        {
            var org = await _context.Organisations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrganisationId == orgId);

            if (org == null) return 0.28f;

            var setting = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == org.UserId);

            return setting?.ElectricityCostPerKwh ?? 0.28f;
        }

        private static List<DeviceBucketsDto> GroupToDeviceSeries(
            List<(Guid deviceId, string name, string type, DeviceBucketDto bucket)> rows)
        {
            return rows
                .GroupBy(r => r.deviceId)
                .Select(g => new DeviceBucketsDto
                {
                    DeviceId = g.Key,
                    DeviceName = g.First().name,
                    DeviceType = g.First().type,
                    Buckets = g.Select(r => r.bucket).OrderBy(b => b.Timestamp).ToList()
                })
                .ToList();
        }
    }
}