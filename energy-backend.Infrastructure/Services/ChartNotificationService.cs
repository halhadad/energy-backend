using energy_backend.Application.Interfaces;
using energy_backend.Application.Models.SignalR;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using Microsoft.Extensions.Logging;
namespace energy_backend.Infrastructure.Services;

public class ChartNotificationService : IRealTimeDataStreamService
{
    private readonly IHubNotificationService _hub;
    private readonly ILogger<ChartNotificationService> _logger;
    public ChartNotificationService(
        IHubNotificationService hub,
        ILogger<ChartNotificationService> logger)
    {
        _hub = hub;
        _logger = logger;
    }
    public async Task SubscribeToChart(string connectionId, Guid orgId, string range)
    {
        if (!IsValidRange(range))
        {
            await _hub.SendErrorAsync(connectionId, $"Invalid chart range: {range}");
            return;
        }
        var groupName = GroupName(orgId, range);
        await _hub.AddToChartGroupAsync(connectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} subscribed to {GroupName}", connectionId, groupName);
    }
    public async Task UnsubscribeFromChart(string connectionId, Guid orgId, string range)
    {
        if (!IsValidRange(range)) return;
        await _hub.RemoveFromChartGroupAsync(connectionId, GroupName(orgId, range));
    }
    public async Task NotifyMinuteAggregateUpdated(Guid orgId, AggregateMinuteEnergy minuteAggregate)
        => await SendAggregateAsync(orgId, "minute", minuteAggregate);
    public async Task NotifyHourAggregateUpdated(Guid orgId, AggregateHourEnergy hourAggregate)
        => await SendAggregateAsync(orgId, "hour", hourAggregate);
    public async Task NotifyDayAggregateUpdated(Guid orgId, AggregateDayEnergy dayAggregate)
        => await SendAggregateAsync(orgId, "day", dayAggregate);
    public async Task NotifyMonthAggregateUpdated(Guid orgId, AggregateMonthEnergy monthAggregate)
        => await SendAggregateAsync(orgId, "month", monthAggregate);
    private async Task SendAggregateAsync(Guid orgId, string range, IEnergyAggregate aggregate)
    {
        await _hub.SendRealTimeChartUpdateAsync(GroupName(orgId, range), new RealTimeEnergyIntervalDto
        {
            OrgId = orgId,
            Range = range,
            IntervalSizeMinutes = IntervalSizeMinutes(range),
            Sequence = DateTime.UtcNow.Ticks,
            Version = DateTime.UtcNow.Ticks,
            Intervals = new List<EnergyIntervalSummaryDto>
            {
                new()
                {
                    Timestamp      = aggregate.Timestamp,
                    Label          = Label(aggregate.Timestamp, range),
                    TotalEnergy    = (decimal)aggregate.TotalActiveEnergyKwh,
                    AverageWatts   = aggregate.AverageActivePowerWatts,
                    AverageVoltage = aggregate.AverageVoltageVolts,
                    TotalCurrent   = aggregate.AverageCurrentAmps,
                    EstimatedCost  = 0
                }
            }
        });
    }
    private static string GroupName(Guid orgId, string range) => $"chart:org:{orgId}:{range}";
    private static bool IsValidRange(string range) => range is "minute" or "hour" or "day" or "month";
    private static int IntervalSizeMinutes(string range) => range switch
    {
        "minute" => 1,
        "hour" => 60,
        "day" => 1440,
        "month" => 43200,
        _ => 0
    };
    private static string Label(DateTime timestamp, string range) => range switch
    {
        "minute" => timestamp.ToString("HH:mm:ss"),
        "hour" => timestamp.ToString("HH:mm"),
        "day" => timestamp.ToString("ddd d"),
        "month" => timestamp.ToString("MMM yy"),
        _ => timestamp.ToString("O")
    };
}