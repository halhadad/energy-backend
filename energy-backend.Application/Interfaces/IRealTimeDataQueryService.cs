using energy_backend.Application.Models.SignalR;

namespace energy_backend.Application.Services
{
    public interface IRealTimeDataQueryService
    {
        // ── Org-level (totalled across all devices) ───────────────────────────

        Task<List<RealTimeChartBucketDto>> GetAggregateMinuteEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to);
        Task<List<RealTimeChartBucketDto>> GetAggregateHourEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to);
        Task<List<RealTimeChartBucketDto>> GetAggregateDayEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to);

        Task<RealTimeChartBucketDto?> GetLatestAggregateMinuteEnergyForOrgAsync(Guid orgId);
        Task<RealTimeChartBucketDto?> GetLatestAggregateHourEnergyForOrgAsync(Guid orgId);
        Task<RealTimeChartBucketDto?> GetLatestAggregateDayEnergyForOrgAsync(Guid orgId);
        Task<RealTimeChartBucketDto?> GetLatestAggregateMonthEnergyForOrgAsync(Guid orgId);

        // ── Per-device breakdowns ─────────────────────────────────────────────

        Task<List<DeviceBucketsDto>> GetDeviceMinuteBucketsForOrgAsync(Guid orgId, DateTime from, DateTime to);
        Task<List<DeviceBucketsDto>> GetDeviceHourBucketsForOrgAsync(Guid orgId, DateTime from, DateTime to);
        Task<List<DeviceBucketsDto>> GetDeviceDayBucketsForOrgAsync(Guid orgId, DateTime from, DateTime to);
        Task<List<DeviceBucketsDto>> GetLatestDeviceMinuteBucketsForOrgAsync(Guid orgId);
    }
}