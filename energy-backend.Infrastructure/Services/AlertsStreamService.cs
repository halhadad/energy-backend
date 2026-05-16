using System;
using System.Threading.Tasks;
using energy_backend.Application.Services; // For IAlertsStreamService, IHubNotificationService, IAlertQueryService
using energy_backend.Core.Entities; // For Alert, AlertEvent
using Microsoft.Extensions.Logging;

// NOTE: This service is now in the API/Presentation layer (energy-backend project)
// It depends on Application layer interfaces for Hub notifications and data querying.

namespace energy_backend.Infrastructure.Services // This is in the API layer
{
    public class AlertsStreamService : IAlertStreamService // Renamed from AlertStreamService
    {
        private readonly IHubNotificationService _hubNotificationService;
        private readonly IAlertQueryService _alertQueryService; // Injected for data query
        private readonly ILogger<AlertsStreamService> _logger;

        public AlertsStreamService(
            IHubNotificationService hubNotificationService,
            IAlertQueryService alertQueryService,
            ILogger<AlertsStreamService> logger)
        {
            _hubNotificationService = hubNotificationService;
            _alertQueryService = alertQueryService;
            _logger = logger;
        }

        public async Task SubscribeToAlerts(string connectionId, Guid orgId)
        {
            var groupName = GetAlertsGroupName(orgId);
            await _hubNotificationService.AddToAlertGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} subscribed to alerts for OrgId {OrgId}", connectionId, orgId);
            // Optionally, send current active alerts to the new subscriber
            // var activeAlerts = await _alertQueryService.GetActiveAlertsForOrganisationAsync(orgId);
            // foreach (var alert in activeAlerts)
            // {
            //    await _hubNotificationService.SendAlertUpdatedAsync(connectionId, alert); // Or a specific method
            // }
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

        private string GetAlertsGroupName(Guid orgId) => $"alerts:org:{orgId}";
    }
}
