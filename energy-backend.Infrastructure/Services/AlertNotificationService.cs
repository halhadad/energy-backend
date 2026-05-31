using energy_backend.Application.Interfaces;
using energy_backend.Core.Entities;
using Microsoft.Extensions.Logging;
namespace energy_backend.Infrastructure.Services
{
    public class AlertNotificationService : IAlertStreamService
    {
        private readonly IHubNotificationService _hubNotificationService;
        private readonly ILogger<AlertNotificationService> _logger;
        public AlertNotificationService(
            IHubNotificationService hubNotificationService,
            ILogger<AlertNotificationService> logger)
        {
            _hubNotificationService = hubNotificationService;
            _logger = logger;
        }
        public async Task SubscribeToAlerts(string connectionId, Guid orgId)
        {
            var groupName = GetAlertsGroupName(orgId);
            await _hubNotificationService.AddToAlertGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} subscribed to alerts for OrgId {OrgId}", connectionId, orgId);
        }
        public async Task UnsubscribeFromAlerts(string connectionId, Guid orgId)
        {
            var groupName = GetAlertsGroupName(orgId);
            await _hubNotificationService.RemoveFromAlertGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} unsubscribed from alerts for OrgId {OrgId}", connectionId, orgId);
        }
        public async Task NotifyAlertTriggered(Guid orgId, AlertEvent alertEvent)
        {
            var groupName = GetAlertsGroupName(orgId);
            await _hubNotificationService.SendAlertTriggeredAsync(groupName, alertEvent);
            _logger.LogInformation("Alert triggered for OrgId {OrgId}: {AlertName}", orgId, alertEvent.Name);
        }
        public async Task NotifyAlertResolved(Guid orgId, Alert alert)
        {
            var groupName = GetAlertsGroupName(orgId);
            await _hubNotificationService.SendAlertResolvedAsync(groupName, alert);
            _logger.LogInformation("Alert resolved for OrgId {OrgId}: {AlertName}", orgId, alert.Name);
        }
        public async Task NotifyAlertUpdated(Guid orgId, Alert alert)
        {
            var groupName = GetAlertsGroupName(orgId);
            await _hubNotificationService.SendAlertUpdatedAsync(groupName, alert);
            _logger.LogInformation("Alert updated for OrgId {OrgId}: {AlertName}", orgId, alert.Name);
        }
        private static string GetAlertsGroupName(Guid orgId) => $"alerts:org:{orgId}";
    }
}