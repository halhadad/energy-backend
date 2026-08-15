using energy_backend.Core.Entities;

namespace energy_backend.Application.Interfaces;

public interface IMockDataAggregationService
{
    Task UpsertBucketsAsync(EnergyReading reading);
    Task NotifyOrgAsync(Guid orgId, IEnumerable<Guid> deviceIds, DateTime slotTimestamp);
}