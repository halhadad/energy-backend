using energy_backend.Core.Entities;

namespace energy_backend.Application.Services
{
    public interface IAggregationCoordinatorService
    {
        /// <summary>
        /// Upserts all aggregate buckets AND fires one SignalR notification.
        /// Use for single-device scenarios.
        /// </summary>
        Task ProcessEnergyReadingAsync(EnergyReading energyReading);

        /// <summary>
        /// Upserts minute/hour/day/month buckets for one reading.
        /// Does NOT fire SignalR. Call in a loop for all devices in a slot,
        /// then call NotifyOrgAsync once per org.
        /// </summary>
        Task UpsertBucketsAsync(EnergyReading energyReading);

        /// <summary>
        /// Queries the org-level sum and fires ONE SignalR chart update.
        /// Call after all UpsertBucketsAsync calls for a slot are complete.
        /// </summary>
        Task NotifyOrgAsync(Guid deviceId, DateTime ts);
    }
}