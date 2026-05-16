using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using energy_backend.Core.Entities;
using energy_backend.Application.Models.SignalR; // For RealTimeChartDataDto

namespace energy_backend.Application.Services
{
    // Abstraction for querying real-time data from infrastructure
    public interface IRealTimeDataQueryService
    {
        Task<List<RealTimeChartBucketDto>> GetAggregateMinuteEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to);
        Task<List<RealTimeChartBucketDto>> GetAggregateHourEnergyForOrgAsync(Guid orgId, DateTime from, DateTime to);
        Task<RealTimeChartBucketDto?> GetLatestAggregateMinuteEnergyForOrgAsync(Guid orgId);
        Task<RealTimeChartBucketDto?> GetLatestAggregateHourEnergyForOrgAsync(Guid orgId);
        Task<RealTimeChartBucketDto?> GetLatestAggregateDayEnergyForOrgAsync(Guid orgId);
        Task<RealTimeChartBucketDto?> GetLatestAggregateMonthEnergyForOrgAsync(Guid orgId);

        // This is for the RealTimeService's GetOverviewDataAsync - will need to be refactored
        Task<RealTimeDto> GetOverviewDataAsync(Guid organisationId);
    }
}
