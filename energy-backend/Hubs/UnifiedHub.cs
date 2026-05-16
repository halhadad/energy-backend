using System.Security.Claims;
using energy_backend.Application.Services;
using energy_backend.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace energy_backend.Hubs
{
    [Authorize]
    public class UnifiedHub : Hub, IEnergyHub   //  add IEnergyHub here
    {
        private readonly IRealTimeDataStreamService _chartStream;
        private readonly IAlertStreamService _alertStream;
        private readonly IOrganisationRepository _orgRepo;
        private readonly ILogger<UnifiedHub> _logger;

        public UnifiedHub(
            IRealTimeDataStreamService chartStream,
            IAlertStreamService alertStream,
            IOrganisationRepository orgRepo,
            ILogger<UnifiedHub> logger)
        {
            _chartStream = chartStream;
            _alertStream = alertStream;
            _orgRepo = orgRepo;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                _logger.LogWarning("UnifiedHub: connection {Id} has no valid user claim", Context.ConnectionId);
                Context.Abort();
                return;
            }
            await base.OnConnectedAsync();
            _logger.LogInformation("UnifiedHub: user {UserId} connected ({ConnId})", userId, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            _logger.LogInformation("UnifiedHub: connection {ConnId} disconnected", Context.ConnectionId);
        }

        public async Task SubscribeToChart(Guid orgId, string range)
        {
            if (!await AuthorizeOrg(orgId)) return;
            await _chartStream.SubscribeToChart(Context.ConnectionId, orgId, range);
        }

        public async Task UnsubscribeFromChart(Guid orgId, string range)
        {
            if (!await AuthorizeOrg(orgId)) return;
            await _chartStream.UnsubscribeFromChart(Context.ConnectionId, orgId, range);
        }

        public async Task SubscribeToAlerts(Guid orgId)
        {
            if (!await AuthorizeOrg(orgId)) return;
            await _alertStream.SubscribeToAlerts(Context.ConnectionId, orgId);
        }

        public async Task UnsubscribeFromAlerts(Guid orgId)
        {
            if (!await AuthorizeOrg(orgId)) return;
            await _alertStream.UnsubscribeFromAlerts(Context.ConnectionId, orgId);
        }

        private Guid? GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        private async Task<bool> AuthorizeOrg(Guid orgId)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return false;
            }
            var org = await _orgRepo.GetByIdAsync(userId.Value, orgId);
            if (org == null)
            {
                await Clients.Caller.SendAsync("Error", $"Organization {orgId} not found or not owned by you");
                return false;
            }
            return true;
        }
    }
}