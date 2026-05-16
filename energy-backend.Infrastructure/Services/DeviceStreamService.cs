using System;
using System.Threading.Tasks;
using energy_backend.Application.Services; // For IDeviceStreamService, IHubNotificationService, IDeviceQueryService
using Microsoft.Extensions.Logging;

// NOTE: This service is now in the API/Presentation layer (energy-backend project)
// It depends on Application layer interfaces for Hub notifications and data querying.

namespace energy_backend.Infrastructure.Services // This is in the API layer
{
    public class DeviceStreamService : IDeviceStreamService
    {
        private readonly IHubNotificationService _hubNotificationService;
        private readonly IDeviceQueryService _deviceQueryService; // Injected for data query
        private readonly ILogger<DeviceStreamService> _logger;

        public DeviceStreamService(
            IHubNotificationService hubNotificationService,
            IDeviceQueryService deviceQueryService,
            ILogger<DeviceStreamService> logger)
        {
            _hubNotificationService = hubNotificationService;
            _deviceQueryService = deviceQueryService;
            _logger = logger;
        }

        public async Task SubscribeToDevices(string connectionId, Guid orgId)
        {
            var groupName = GetDevicesGroupName(orgId);
            await _hubNotificationService.AddToDeviceGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} subscribed to devices for OrgId {OrgId}", connectionId, orgId);
            // Example: optionally send initial device list
            // var devices = await _deviceQueryService.GetDevicesForOrganisationAsync(orgId);
            // await _hubNotificationService.SendDeviceListAsync(connectionId, devices); // Assuming SendDeviceListAsync exists
        }

        public async Task UnsubscribeFromDevices(string connectionId, Guid orgId)
        {
            var groupName = GetDevicesGroupName(orgId);
            await _hubNotificationService.RemoveFromDeviceGroupAsync(connectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} unsubscribed from devices for OrgId {OrgId}", connectionId, orgId);
        }

        private string GetDevicesGroupName(Guid orgId) => $"devices:org:{orgId}";

        // Add methods for device-specific updates if needed later
    }
}
