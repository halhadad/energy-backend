using energy_backend.Api.Hubs;
using energy_backend.Application.Interfaces;
using energy_backend.Application.Models.SignalR;
using energy_backend.Core.Entities;
using Microsoft.AspNetCore.SignalR;

namespace energy_backend.Api.Services;

public class HubNotificationService(
    IHubContext<UnifiedHub> hubContext,
    ILogger<HubNotificationService> logger) : IHubNotificationService
{
    // Chart group
    public async Task SendRealTimeChartUpdateAsync(string groupName, RealTimeEnergyIntervalDto data)
    {
        await hubContext.Clients.Group(groupName).SendAsync("ReceiveChartUpdate", data);
        logger.LogDebug("Sent chart update to group {GroupName}", groupName);
    }

    public Task AddToChartGroupAsync(string connectionId, string groupName)
        => hubContext.Groups.AddToGroupAsync(connectionId, groupName);

    public Task RemoveFromChartGroupAsync(string connectionId, string groupName)
        => hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);

    // Live tick group
    public async Task SendLiveTickAsync(string groupName, LiveTickDto tick)
    {
        await hubContext.Clients.Group(groupName).SendAsync("ReceiveLiveTick", tick);
        logger.LogDebug("Sent live tick to group {GroupName} at {Timestamp:O}", groupName, tick.Timestamp);
    }

    public Task AddToLiveGroupAsync(string connectionId, string groupName)
        => hubContext.Groups.AddToGroupAsync(connectionId, groupName);

    public Task RemoveFromLiveGroupAsync(string connectionId, string groupName)
        => hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);

    // Historical "data changed" signal group
    public Task AddToHistoricalGroupAsync(string connectionId, string groupName)
        => hubContext.Groups.AddToGroupAsync(connectionId, groupName);

    public Task RemoveFromHistoricalGroupAsync(string connectionId, string groupName)
        => hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);

    public Task SendHistoricalUpdateAsync(string groupName, object payload)
        => hubContext.Clients.Group(groupName).SendAsync("ReceiveHistoricalUpdate", payload);

    // Alert group
    public Task SendAlertTriggeredAsync(string groupName, AlertEvent alertEvent)
        => hubContext.Clients.Group(groupName).SendAsync("alert-triggered", new
        {
            alertEvent.AlertEventId,
            alertEvent.AlertId,
            alertEvent.OrganisationId,
            alertEvent.Name,
            alertEvent.ThresholdValue,
            alertEvent.TriggeredValueWatts,
            alertEvent.TriggeredAt,
            alertEvent.IsAcknowledged,
        });

    public Task SendAlertResolvedAsync(string groupName, Alert alert)
        => hubContext.Clients.Group(groupName).SendAsync("alert-resolved", new
        {
            alert.AlertId,
            status = "Resolved",
            alert.ResolvedAt,
        });

    public Task SendAlertUpdatedAsync(string groupName, Alert alert)
        => hubContext.Clients.Group(groupName).SendAsync("alert-updated", new
        {
            alert.AlertId,
            status = alert.IsActive ? "Triggered" : alert.ResolvedAt is not null ? "Resolved" : "Monitoring",
            alert.IsActive,
            alert.ResolvedAt,
        });

    public Task AddToAlertGroupAsync(string connectionId, string groupName)
        => hubContext.Groups.AddToGroupAsync(connectionId, groupName);

    public Task RemoveFromAlertGroupAsync(string connectionId, string groupName)
        => hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);

    public Task SendErrorAsync(string connectionId, string message)
        => hubContext.Clients.Client(connectionId).SendAsync("Error", message);
}
