using energy_backend.Core.Enums;
using energy_backend.Core.Projections;

namespace energy_backend.Core.Interfaces;

public interface IAnalyticsRepository
{
    Task<List<AggregateMetricRow>> GetAggregateMetricsAsync(
        Guid orgId, DateTime start, DateTime end, TimeGranularity granularity);
    Task<List<DeviceSnapshotRow>> GetDeviceSnapshotMetricsAsync(
        Guid orgId, DateTime start, DateTime end, TimeGranularity granularity);
    Task<AggregateMetricRow?> GetLatestMetricSnapshotAsync(
        Guid orgId, TimeGranularity granularity);
}
