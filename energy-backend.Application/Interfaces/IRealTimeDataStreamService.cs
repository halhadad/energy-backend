using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using energy_backend.Core.Entities; // For aggregate entities
using energy_backend.Application.Models.SignalR; // For RealTimeChartDataDto

namespace energy_backend.Application.Services // Note: This file has been moved to energy-backend/Services/
{
    public interface IRealTimeDataStreamService // Renamed
    {
        Task SubscribeToChart(string connectionId, Guid orgId, string range);
        Task UnsubscribeFromChart(string connectionId, Guid orgId, string range);

        // Method for event-driven updates (to be called by AggregationCoordinatorService)
        Task NotifyMinuteAggregateUpdated(Guid orgId, AggregateMinuteEnergy minuteAggregate);
        Task NotifyHourAggregateUpdated(Guid orgId, AggregateHourEnergy hourAggregate); // For higher-level updates
        Task NotifyDayAggregateUpdated(Guid orgId, AggregateDayEnergy dayAggregate);
        Task NotifyMonthAggregateUpdated(Guid orgId, AggregateMonthEnergy monthAggregate);
    }
}
