using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using energy_backend.Application.Services; // For streaming services
using energy_backend.Core.Interfaces; // For IOrganisationRepository

namespace energy_backend.Hubs
{
    [Authorize]
    public class UnifiedHub : Hub
    {
        private readonly IRealTimeDataStreamService _realTimeDataStreamService;
        private readonly IAlertStreamService _alertStreamService;
        private readonly IDeviceStreamService _deviceStreamService; // Placeholder
        private readonly IOrganisationRepository _organisationRepository; // Injected
        private readonly ILogger<UnifiedHub> _logger;

        public UnifiedHub(
            IRealTimeDataStreamService realTimeDataStreamService,
            IAlertStreamService alertStreamService,
            IDeviceStreamService deviceStreamService, // Placeholder
            IOrganisationRepository organisationRepository, // Injected
            ILogger<UnifiedHub> logger)
        {
            _realTimeDataStreamService = realTimeDataStreamService;
            _alertStreamService = alertStreamService;
            _deviceStreamService = deviceStreamService; // Placeholder
            _organisationRepository = organisationRepository; // Injected
            _logger = logger;
        }


        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                _logger.LogWarning("Connection {ConnectionId} has no HTTP context. Aborting.", Context.ConnectionId);
                Context.Abort();
                return;
            }
            _logger.LogDebug("Connection {ConnectionId} HTTP context found.", Context.ConnectionId);

            // OrgId is not required at connection time, but UserId is for authentication
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? Context.User?.FindFirst("sub")?.Value;

            _logger.LogDebug("Connection {ConnectionId} UserIdClaim: '{UserIdClaim}'", Context.ConnectionId, userIdClaim);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("Connection {ConnectionId} missing or empty UserIdClaim. Aborting. UserIdClaim: '{UserIdClaim}'", Context.ConnectionId, userIdClaim);
                Context.Abort();
                return;
            }
            _logger.LogDebug("Connection {ConnectionId} UserIdClaim found: '{UserIdClaim}'", Context.ConnectionId, userIdClaim);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Connection {ConnectionId} invalid UserIdClaim format. Aborting. UserIdClaim: '{UserIdClaim}'", Context.ConnectionId, userIdClaim);
                Context.Abort();
                return;
            }
            _logger.LogDebug("Connection {ConnectionId} UserId successfully parsed: '{UserId}'", Context.ConnectionId, userId);

            // User is authenticated and userId is valid. No org-specific actions here.
            // Org-specific group assignments will happen on explicit subscribes (e.g., SubscribeToChart)

            await base.OnConnectedAsync();
            _logger.LogInformation("Connection {ConnectionId} OnConnectedAsync completed successfully. User ID: {UserId}", Context.ConnectionId, userId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // The streaming services will handle removal from their specific groups
            // Generic org group removal
            var orgClaim = Context.User?.FindFirst("orgId")?.Value
                           ?? Context.User?.FindFirst("org")?.Value;
            if (Guid.TryParse(orgClaim, out var orgId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org:{orgId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // --- Chart Streaming Methods ---
        public async Task SubscribeToChart(Guid orgId, string range)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
            {
                _logger.LogWarning("SubscribeToChart: Connection {ConnectionId} has no valid UserId claim. Aborting subscription to org {OrgId}, range {Range}.", Context.ConnectionId, orgId, range);
                await Clients.Caller.SendAsync("Error", "Unauthorized: No valid user ID found for subscription.");
                return;
            }

            if (await _organisationRepository.GetByIdAsync(parsedUserId, orgId) == null)
            {
                _logger.LogWarning("SubscribeToChart: User {UserId} is not authorized for OrgId {OrgId}. Aborting subscription to range {Range}.", parsedUserId, orgId, range);
                await Clients.Caller.SendAsync("Error", $"Unauthorized: User does not own organization {orgId}.");
                return;
            }

            await _realTimeDataStreamService.SubscribeToChart(Context.ConnectionId, orgId, range);
            _logger.LogInformation("Client {ConnectionId} ({UserId}) successfully subscribed to chart for OrgId {OrgId}, range {Range}", Context.ConnectionId, parsedUserId, orgId, range);
        }

        public async Task UnsubscribeFromChart(Guid orgId, string range)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
            {
                _logger.LogWarning("UnsubscribeFromChart: Connection {ConnectionId} has no valid UserId claim. Aborting unsubscription from org {OrgId}, range {Range}.", Context.ConnectionId, orgId, range);
                return;
            }

            if (await _organisationRepository.GetByIdAsync(parsedUserId, orgId) == null)
            {
                _logger.LogWarning("UnsubscribeFromChart: User {UserId} is not authorized for OrgId {OrgId}. Aborting unsubscription from range {Range}.", parsedUserId, orgId, range);
                return;
            }

            await _realTimeDataStreamService.UnsubscribeFromChart(Context.ConnectionId, orgId, range);
            _logger.LogInformation("Client {ConnectionId} ({UserId}) successfully unsubscribed from chart for OrgId {OrgId}, range {Range}", Context.ConnectionId, parsedUserId, orgId, range);
        }

        // --- Alert Streaming Methods ---
        public async Task SubscribeToAlerts(Guid orgId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
            {
                _logger.LogWarning("SubscribeToAlerts: Connection {ConnectionId} has no valid UserId claim. Aborting subscription to alerts for org {OrgId}.", Context.ConnectionId, orgId);
                await Clients.Caller.SendAsync("Error", "Unauthorized: No valid user ID found for subscription.");
                return;
            }

            if (await _organisationRepository.GetByIdAsync(parsedUserId, orgId) == null)
            {
                _logger.LogWarning("SubscribeToAlerts: User {UserId} is not authorized for OrgId {OrgId}. Aborting subscription to alerts.", parsedUserId, orgId);
                await Clients.Caller.SendAsync("Error", $"Unauthorized: User does not own organization {orgId}.");
                return;
            }

            await _alertStreamService.SubscribeToAlerts(Context.ConnectionId, orgId);
            _logger.LogInformation("Client {ConnectionId} ({UserId}) successfully subscribed to alerts for OrgId {OrgId}", Context.ConnectionId, parsedUserId, orgId);
        }

        public async Task UnsubscribeFromAlerts(Guid orgId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
            {
                _logger.LogWarning("UnsubscribeFromAlerts: Connection {ConnectionId} has no valid UserId claim. Aborting unsubscription from alerts for org {OrgId}.", Context.ConnectionId, orgId);
                return;
            }

            if (await _organisationRepository.GetByIdAsync(parsedUserId, orgId) == null)
            {
                _logger.LogWarning("UnsubscribeFromAlerts: User {UserId} is not authorized for OrgId {OrgId}. Aborting unsubscription from alerts.", parsedUserId, orgId);
                return;
            }

            await _alertStreamService.UnsubscribeFromAlerts(Context.ConnectionId, orgId);
            _logger.LogInformation("Client {ConnectionId} ({UserId}) successfully unsubscribed from alerts for OrgId {OrgId}", Context.ConnectionId, parsedUserId, orgId);
        }

        // --- Device Streaming Methods (Placeholder) ---
        public async Task SubscribeToDevices(Guid orgId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
            {
                _logger.LogWarning("SubscribeToDevices: Connection {ConnectionId} has no valid UserId claim. Aborting subscription to devices for org {OrgId}.", Context.ConnectionId, orgId);
                await Clients.Caller.SendAsync("Error", "Unauthorized: No valid user ID found for subscription.");
                return;
            }

            if (await _organisationRepository.GetByIdAsync(parsedUserId, orgId) == null)
            {
                _logger.LogWarning("SubscribeToDevices: User {UserId} is not authorized for OrgId {OrgId}. Aborting subscription to devices.", parsedUserId, orgId);
                await Clients.Caller.SendAsync("Error", $"Unauthorized: User does not own organization {orgId}.");
                return;
            }

            await _deviceStreamService.SubscribeToDevices(Context.ConnectionId, orgId);
            _logger.LogInformation("Client {ConnectionId} ({UserId}) successfully subscribed to devices for OrgId {OrgId}", Context.ConnectionId, parsedUserId, orgId);
        }

        public async Task UnsubscribeFromDevices(Guid orgId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
            {
                _logger.LogWarning("UnsubscribeFromDevices: Connection {ConnectionId} has no valid UserId claim. Aborting unsubscription from devices for org {OrgId}.", Context.ConnectionId, orgId);
                return;
            }

            if (await _organisationRepository.GetByIdAsync(parsedUserId, orgId) == null)
            {
                _logger.LogWarning("UnsubscribeFromDevices: User {UserId} is not authorized for OrgId {OrgId}. Aborting unsubscription from devices.", parsedUserId, orgId);
                return;
            }
            await _deviceStreamService.UnsubscribeFromDevices(Context.ConnectionId, orgId);
            _logger.LogInformation("Client {ConnectionId} ({UserId}) successfully unsubscribed from devices for OrgId {OrgId}", Context.ConnectionId, parsedUserId, orgId);
        }
    }
}
