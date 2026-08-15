using energy_backend.Application.Interfaces;
using energy_backend.Application.Models.SignalR;
using energy_backend.Core.Entities;
using energy_backend.Core.Enums;
using energy_backend.Core.Interfaces;
using energy_backend.Core.Services;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services;

public class ChartNotificationService(
    IHubNotificationService hub,
    IEnergyRateRepository rateRepo,
    ILogger<ChartNotificationService> logger) : IRealTimeDataStreamService
{
    // Live group: raw readings. The HTTP snapshot endpoint seeds the chart on page load,
    // so no catch-up push is needed on subscribe.
    public async Task SubscribeToLive(string connectionId, Guid orgId)
    {
        await hub.AddToLiveGroupAsync(connectionId, LiveGroupName(orgId));
        logger.LogInformation("Client {ConnectionId} subscribed to live ticks for OrgId={OrgId}",
            connectionId, orgId);
    }

    public Task UnsubscribeFromLive(string connectionId, Guid orgId)
        => hub.RemoveFromLiveGroupAsync(connectionId, LiveGroupName(orgId));

    public async Task NotifyLiveTickAsync(
        Guid orgId, IEnumerable<EnergyReading> orgReadings, decimal ratePerKwh)
    {
        var readings = orgReadings.ToList();
        if (readings.Count == 0) return;

        var ts = DateTime.SpecifyKind(readings.First().Timestamp, DateTimeKind.Utc);
        var totalWatts = readings.Sum(r => r.ActivePowerWatts);
        var avgVoltage = readings.Average(r => r.VoltageVolts);
        var totalCurrent = readings.Sum(r => r.CurrentAmps);
        var avgPf = readings.Average(r => r.PowerFactor);

        await hub.SendLiveTickAsync(LiveGroupName(orgId), new LiveTickDto
        {
            OrgId = orgId,
            Timestamp = ts,
            ActivePowerWatts = totalWatts,
            VoltageVolts = avgVoltage,
            CurrentAmps = totalCurrent,
            PowerFactor = avgPf,
            CostRatePerHour = (decimal)(totalWatts / 1000.0) * ratePerKwh
        });
    }

    // Chart group: aggregate rollup notifications.
    public async Task SubscribeToChart(string connectionId, Guid orgId, string range)
    {
        if (!IsValidRange(range))
        {
            await hub.SendErrorAsync(connectionId, $"Invalid chart range: {range}");
            return;
        }
        await hub.AddToChartGroupAsync(connectionId, ChartGroupName(orgId, range));
        logger.LogInformation("Client {ConnectionId} subscribed to chart:{Range} for OrgId={OrgId}",
            connectionId, range, orgId);
    }

    public Task UnsubscribeFromChart(string connectionId, Guid orgId, string range)
    {
        if (!IsValidRange(range)) return Task.CompletedTask;
        return hub.RemoveFromChartGroupAsync(connectionId, ChartGroupName(orgId, range));
    }

    // Historical page: clients join and get a lightweight "data changed" signal to refetch.
    public Task SubscribeToHistorical(string connectionId, Guid orgId)
        => hub.AddToHistoricalGroupAsync(connectionId, HistoricalGroupName(orgId));

    public Task UnsubscribeFromHistorical(string connectionId, Guid orgId)
        => hub.RemoveFromHistoricalGroupAsync(connectionId, HistoricalGroupName(orgId));

    public Task NotifyHistoricalUpdatedAsync(Guid orgId)
        => hub.SendHistoricalUpdateAsync(HistoricalGroupName(orgId),
            new { OrgId = orgId, UpdatedAt = DateTime.UtcNow });

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
        var ts = DateTime.SpecifyKind(aggregate.Timestamp, DateTimeKind.Utc);
        var rate = await rateRepo.GetCurrentRateAsync(orgId);
        var cost = EnergyCalculator.CalculateEstimatedCost(aggregate.TotalActiveEnergyKwh, rate);

        await hub.SendRealTimeChartUpdateAsync(ChartGroupName(orgId, range), new RealTimeEnergyIntervalDto
        {
            OrgId = orgId,
            Range = range,
            IntervalSizeMinutes = IntervalSizeMinutes(range),
            Sequence = DateTime.UtcNow.Ticks,
            Version = DateTime.UtcNow.Ticks,
            Intervals =
            [
                new EnergyIntervalSummaryDto
                {
                    Timestamp      = ts,
                    Label          = Label(ts, range),
                    TotalEnergy    = (decimal)aggregate.TotalActiveEnergyKwh,
                    AverageWatts   = aggregate.AverageActivePowerWatts,
                    AverageVoltage = aggregate.AverageVoltageVolts,
                    TotalCurrent   = aggregate.AverageCurrentAmps,
                    EstimatedCost  = cost
                }
            ]
        });
    }

    private static string LiveGroupName(Guid orgId) => $"live:org:{orgId}";
    private static string ChartGroupName(Guid orgId, string r) => $"chart:org:{orgId}:{r}";
    private static string HistoricalGroupName(Guid orgId) => $"historical:org:{orgId}";
    private static bool IsValidRange(string r) => r is "minute" or "hour" or "day" or "month";
    private static int IntervalSizeMinutes(string r) => r switch
    {
        "minute" => 1,
        "hour" => 60,
        "day" => 1440,
        "month" => 43200,
        _ => 0
    };
    private static string Label(DateTime ts, string r) => r switch
    {
        "minute" => ts.ToString("HH:mm:ss"),
        "hour" => ts.ToString("HH:mm"),
        "day" => ts.ToString("ddd d"),
        "month" => ts.ToString("MMM yy"),
        _ => ts.ToString("O")
    };
}