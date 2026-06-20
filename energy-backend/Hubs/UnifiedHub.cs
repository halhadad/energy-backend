using System.Security.Claims;
using energy_backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace energy_backend.Api.Hubs;

[Authorize]
public class UnifiedHub : Hub
{
    private readonly IRealTimeDataStreamService _chartStream;
    private readonly IAlertStreamService _alertStream;
    private readonly IOrganisationService _orgService;
    private readonly ILogger<UnifiedHub> _logger;

    public UnifiedHub(
        IRealTimeDataStreamService chartStream,
        IAlertStreamService alertStream,
        IOrganisationService orgService,
        ILogger<UnifiedHub> logger)
    {
        _chartStream = chartStream;
        _alertStream = alertStream;
        _orgService = orgService;
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

    // Real-time dashboard: client receives "ReceiveLiveTick" every broadcast interval.
    public async Task SubscribeToLive(Guid orgId)
    {
        if (!await AuthorizeOrg(orgId)) return;
        await _chartStream.SubscribeToLive(Context.ConnectionId, orgId);
    }

    public async Task UnsubscribeFromLive(Guid orgId)
    {
        if (!await AuthorizeOrg(orgId)) return;
        await _chartStream.UnsubscribeFromLive(Context.ConnectionId, orgId);
    }

    // Historical / aggregate chart groups (minute / hour / day / month).
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

    // Historical analytics page: join to receive a per-minute "data changed" signal, then refetch.
    public async Task SubscribeToHistorical(Guid orgId)
    {
        if (!await AuthorizeOrg(orgId)) return;
        await _chartStream.SubscribeToHistorical(Context.ConnectionId, orgId);
    }

    public async Task UnsubscribeFromHistorical(Guid orgId)
    {
        if (!await AuthorizeOrg(orgId)) return;
        await _chartStream.UnsubscribeFromHistorical(Context.ConnectionId, orgId);
    }

    // Alerts.
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
        var org = await _orgService.GetByIdAsync(userId.Value, orgId);
        if (org == null)
        {
            await Clients.Caller.SendAsync("Error",
                $"Organisation {orgId} not found or not owned by you");
            return false;
        }
        return true;
    }
}
