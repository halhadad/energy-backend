using System;
using System.Threading.Tasks;
using energy_backend.Application.Models.SignalR;
using energy_backend.Application.Models; // For AggregationRequestDto and AggregationResultDto

namespace energy_backend.Application.Services
{
    public enum AggregationLevel
    {
        Raw,
        FiveSecond, // Current behavior for 'current consumption'
        Minute,
        Hour,
        Day,
        Week,
        Month
    }

    public interface IAggregationService // Renamed from IRealTimeAggregationService
    {
        // This method will be adapted to query new aggregate tables
        Task<RealTimeDto> GetAggregatedOverviewDataAsync(
            Guid organisationId,
            AggregationLevel aggregationLevel,
            DateTime startTime,
            DateTime endTime);

        // New method for generalized aggregation via HTTP endpoint (SnapshotController)
        Task<AggregationResultDto> GetAggregatedDataAsync(AggregationRequestDto request);
    }
}
