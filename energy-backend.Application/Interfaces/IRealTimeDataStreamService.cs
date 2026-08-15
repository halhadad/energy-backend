using energy_backend.Core.Entities;

namespace energy_backend.Application.Interfaces;

public interface IRealTimeDataStreamService
{
    // Aggregate chart subscriptions
    Task SubscribeToChart(string connectionId, Guid orgId, string range);
    Task UnsubscribeFromChart(string connectionId, Guid orgId, string range);

    // Historical analytics page: clients join to receive a "data changed" signal once per
    // minute (as buckets complete) and refetch the snapshot at their chosen preset.
    Task SubscribeToHistorical(string connectionId, Guid orgId);
    Task UnsubscribeFromHistorical(string connectionId, Guid orgId);
    Task NotifyHistoricalUpdatedAsync(Guid orgId);

    // Raw 5-second tick subscription (Real-Time Dashboard page)
    Task SubscribeToLive(string connectionId, Guid orgId);
    Task UnsubscribeFromLive(string connectionId, Guid orgId);

    // Aggregate rollup notifications (chart:org:... groups)
    Task NotifyMinuteAggregateUpdated(Guid orgId, AggregateMinuteEnergy minuteAggregate);
    Task NotifyHourAggregateUpdated(Guid orgId, AggregateHourEnergy hourAggregate);
    Task NotifyDayAggregateUpdated(Guid orgId, AggregateDayEnergy dayAggregate);
    Task NotifyMonthAggregateUpdated(Guid orgId, AggregateMonthEnergy monthAggregate);

    // Pushes one aggregated live tick across all org devices to the live group.
    Task NotifyLiveTickAsync(Guid orgId, IEnumerable<EnergyReading> orgReadings, decimal ratePerKwh);
}
