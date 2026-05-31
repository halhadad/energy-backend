using energy_backend.Core.Entities;

namespace energy_backend.Application.Interfaces;

public interface IRealTimeDataStreamService
{
    Task SubscribeToChart(string connectionId, Guid orgId, string range);
    Task UnsubscribeFromChart(string connectionId, Guid orgId, string range);
    Task NotifyMinuteAggregateUpdated(Guid orgId, AggregateMinuteEnergy minuteAggregate);
    Task NotifyHourAggregateUpdated(Guid orgId, AggregateHourEnergy hourAggregate);
    Task NotifyDayAggregateUpdated(Guid orgId, AggregateDayEnergy dayAggregate);
    Task NotifyMonthAggregateUpdated(Guid orgId, AggregateMonthEnergy monthAggregate);
}
