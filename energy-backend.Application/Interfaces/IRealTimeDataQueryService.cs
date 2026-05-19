using energy_backend.Application.Models.SignalR;

namespace energy_backend.Application.Services
{
    public interface IRealTimeDataQueryService
    {
        Task<List<RealTimeChartBucketDto>> GetAggregateMinuteEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to);
        Task<List<RealTimeChartBucketDto>> GetAggregateHourEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to);

        /// <summary>Day-level buckets — used for catch-up when subscribing to the day chart.</summary>
        Task<List<RealTimeChartBucketDto>> GetAggregateDayEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to);

        Task<RealTimeChartBucketDto?> GetLatestAggregateMinuteEnergyForOrgAsync(Guid orgId);
        Task<RealTimeChartBucketDto?> GetLatestAggregateHourEnergyForOrgAsync(Guid orgId);
        Task<RealTimeChartBucketDto?> GetLatestAggregateDayEnergyForOrgAsync(Guid orgId);
        Task<RealTimeChartBucketDto?> GetLatestAggregateMonthEnergyForOrgAsync(Guid orgId);
    }
}