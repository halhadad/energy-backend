// MOVE: energy-backend.Core/Interfaces/IAnalyticsRepository.cs
//    → energy-backend.Application/Interfaces/IAnalyticsRepository.cs

namespace energy_backend.Application.Interfaces;  // was Core.Interfaces

using energy_backend.Application.Models.Projections;  // now legal — same layer
using energy_backend.Core.Enums;

public interface IAnalyticsRepository
{
    Task<decimal> GetUserCostRateAsync(Guid orgId);
    Task<List<AggregateMetricRow>> GetAggregateMetricsAsync(
        Guid orgId, DateTime start, DateTime end, TimeGranularity granularity);
    Task<List<DeviceSnapshotRow>> GetDeviceSnapshotMetricsAsync(
        Guid orgId, DateTime start, DateTime end, TimeGranularity granularity);
    Task<AggregateMetricRow?> GetLatestMetricSnapshotAsync(
        Guid orgId, TimeGranularity granularity);
}