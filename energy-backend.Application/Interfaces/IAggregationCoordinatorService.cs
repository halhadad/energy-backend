using energy_backend.Core.Entities;

namespace energy_backend.Application.Services
{
    public interface IAggregationCoordinatorService
    {
        /// <summary>
        /// Upserts all aggregate buckets AND fires one SignalR notification.
        /// Use when processing a single reading in isolation.
        /// </summary>
        Task ProcessEnergyReadingAsync(EnergyReading reading);

        /// <summary>
        /// Upserts minute/hour/day/month buckets for one reading.
        /// Does NOT fire SignalR. Use in a batch loop, then call NotifyOrgAsync once.
        /// </summary>
        Task UpsertBucketsAsync(EnergyReading reading);

        /// <summary>
        /// Queries the org-level sum of AveragePowerWatts for the latest minute
        /// bucket and fires one SignalR update. Call once after all UpsertBucketsAsync
        /// calls for a slot are complete.
        /// </summary>
        Task NotifyOrgAsync(Guid deviceId, DateTime slotTimestamp);
    }
}