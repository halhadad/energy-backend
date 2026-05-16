using System;
using System.Threading.Tasks;
using energy_backend.Application.Services;
using energy_backend.Application.Models.SignalR; // For RealTimeChartDataDto
using energy_backend.Core.Entities; // For Alert, AlertEvent
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using energy_backend.Hubs;

namespace energy_backend.Infrastructure.Services 
{
    public class HubNotificationService : IHubNotificationService
    {
        private readonly IHubContext<UnifiedHub> _hubContext;
        private readonly ILogger<HubNotificationService> _logger;

        public HubNotificationService(IHubContext<UnifiedHub> hubContext, ILogger<HubNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        // --- Chart Streaming ---
        public async Task SendRealTimeChartUpdateAsync(string groupName, RealTimeChartDataDto data)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveChartUpdate", data);
            _logger.LogDebug("Sent chart update to group {GroupName} for range {Range}", groupName, data.Range);
        }

        public async Task SendRealTimeChartCatchUpAsync(string connectionId, RealTimeChartDataDto data)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveChartCatchUp", data);
            _logger.LogDebug("Sent chart catch-up data to client {ConnectionId} for range {Range}", connectionId, data.Range);
        }

        public async Task AddToChartGroupAsync(string connectionId, string groupName)
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} added to chart group {GroupName}", connectionId, groupName);
        }

        public async Task RemoveFromChartGroupAsync(string connectionId, string groupName)
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} removed from chart group {GroupName}", connectionId, groupName);
        }

        // --- Alert Streaming ---
        public async Task SendAlertTriggeredAsync(string groupName, AlertEvent alertEvent)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("alert-triggered", alertEvent);
            _logger.LogInformation("Alert triggered event sent to group {GroupName}", groupName);
        }

        public async Task SendAlertResolvedAsync(string groupName, Alert alert)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("alert-resolved", alert);
            _logger.LogInformation("Alert resolved event sent to group {GroupName}", groupName);
        }

        public async Task SendAlertUpdatedAsync(string groupName, Alert alert)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("alert-updated", alert);
            _logger.LogInformation("Alert updated event sent to group {GroupName}", groupName);
        }

        public async Task AddToAlertGroupAsync(string connectionId, string groupName)
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} added to alert group {GroupName}", connectionId, groupName);
        }

        public async Task RemoveFromAlertGroupAsync(string connectionId, string groupName)
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} removed from alert group {GroupName}", connectionId, groupName);
        }

        // --- Device Streaming (Placeholder) ---
        public async Task AddToDeviceGroupAsync(string connectionId, string groupName)
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} added to device group {GroupName}", connectionId, groupName);
        }

        public async Task RemoveFromDeviceGroupAsync(string connectionId, string groupName)
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} removed from device group {GroupName}", connectionId, groupName);
        }

        // General
        public async Task SendErrorAsync(string connectionId, string message)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("Error", message);
            _logger.LogWarning("Sent error to client {ConnectionId}: {Message}", connectionId, message);
        }
    }
}
