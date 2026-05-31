using energy_backend.Application.Interfaces;
using energy_backend.Application.Models.SignalR;
using energy_backend.Infrastructure.Hubs;
using energy_backend.Core.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services;

public class HubNotificationService : IHubNotificationService
{
    private readonly IHubContext<UnifiedHub> _hubContext;
    private readonly ILogger<HubNotificationService> _logger;

    public HubNotificationService(
        IHubContext<UnifiedHub> hubContext,
        ILogger<HubNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendRealTimeChartUpdateAsync(string groupName, RealTimeEnergyIntervalDto data)
    {
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveChartUpdate", data);
        _logger.LogDebug("Sent chart update to group {GroupName}", groupName);
    }

    public async Task SendRealTimeChartCatchUpAsync(string connectionId, RealTimeEnergyIntervalDto data)
    {
        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveChartCatchUp", data);
        _logger.LogDebug("Sent chart catch-up to client {ConnectionId}", connectionId);
    }

    public async Task AddToChartGroupAsync(string connectionId, string groupName)
        => await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);

    public async Task RemoveFromChartGroupAsync(string connectionId, string groupName)
        => await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);

    public async Task SendAlertTriggeredAsync(string groupName, AlertEvent alertEvent)
        => await _hubContext.Clients.Group(groupName).SendAsync("alert-triggered", alertEvent);

    public async Task SendAlertResolvedAsync(string groupName, Alert alert)
        => await _hubContext.Clients.Group(groupName).SendAsync("alert-resolved", alert);

    public async Task SendAlertUpdatedAsync(string groupName, Alert alert)
        => await _hubContext.Clients.Group(groupName).SendAsync("alert-updated", alert);

    public async Task AddToAlertGroupAsync(string connectionId, string groupName)
        => await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);

    public async Task RemoveFromAlertGroupAsync(string connectionId, string groupName)
        => await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);

    public async Task AddToDeviceGroupAsync(string connectionId, string groupName)
        => await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);

    public async Task RemoveFromDeviceGroupAsync(string connectionId, string groupName)
        => await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);

    public async Task SendErrorAsync(string connectionId, string message)
        => await _hubContext.Clients.Client(connectionId).SendAsync("Error", message);
}
