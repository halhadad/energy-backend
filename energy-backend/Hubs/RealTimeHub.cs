using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using energy_backend.Application.Services;
using energy_backend.Infrastructure.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace energy_backend.Hubs
{
    [Authorize]
    public class RealTimeHub : Hub
    {
        private readonly ConnectionTracker _connectionTracker;
        private readonly ILogger<RealTimeHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<RealTimeHub> _hubContext;

        // Track CancellationTokenSources per connection so we can cancel the per-connection loop
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _connectionCts
            = new ConcurrentDictionary<string, CancellationTokenSource>();

        public RealTimeHub(
            ConnectionTracker connectionTracker,
            ILogger<RealTimeHub> logger,
            IServiceScopeFactory scopeFactory,
            IHubContext<RealTimeHub> hubContext)
        {
            _connectionTracker = connectionTracker;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                _logger.LogWarning("Connection {ConnectionId} has no HTTP context. Aborting.", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "No HTTP context available");
                Context.Abort();
                return;
            }

            // Get orgId from claims or query string
            var orgClaim = Context.User?.FindFirst("orgId")?.Value
                           ?? Context.User?.FindFirst("org")?.Value;
            if (string.IsNullOrEmpty(orgClaim))
            {
                orgClaim = httpContext.Request.Query["orgId"].ToString();
            }

            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? Context.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(orgClaim) || string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("Connection {ConnectionId} missing claims. Aborting.", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Missing org or user claim");
                Context.Abort();
                return;
            }

            if (!Guid.TryParse(orgClaim, out var orgId) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Connection {ConnectionId} invalid claim formats. Aborting.", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Invalid org or user claim format");
                Context.Abort();
                return;
            }

            // Add to group for potential targeting
            await Groups.AddToGroupAsync(Context.ConnectionId, orgId.ToString());

            // Track connection
            _connectionTracker.Add(Context.ConnectionId, userId, orgId);

            // Create CancellationTokenSource and start background loop
            var cts = new CancellationTokenSource();
            if (!_connectionCts.TryAdd(Context.ConnectionId, cts))
            {
                _logger.LogWarning("Could not add CTS for connection {ConnectionId}", Context.ConnectionId);
            }

            _ = RunUpdateLoop(Context.ConnectionId, userId, orgId, cts.Token);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionCts.TryRemove(Context.ConnectionId, out var cts))
            {
                try { cts.Cancel(); } catch { }
                try { cts.Dispose(); } catch { }
            }

            _connectionTracker.Remove(Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "unused"); // no-op but safe

            await base.OnDisconnectedAsync(exception);
        }

        private async Task RunUpdateLoop(string connectionId, Guid userId, Guid orgId, CancellationToken token)
        {
            _logger.LogInformation("Started update loop for connection {ConnectionId} (user={UserId} org={OrgId})", connectionId, userId, orgId);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // Create a new scope for each iteration to avoid disposed DbContext
                        using var scope = _scopeFactory.CreateScope();
                        var realTimeService = scope.ServiceProvider.GetRequiredService<IRealTimeService>();

                        var overview = await realTimeService.GetOverviewDataAsync(orgId);

                        await _hubContext.Clients.Client(connectionId)
                            .SendAsync("ReceiveOverviewData", overview, token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while pushing overview to {ConnectionId}", connectionId);
                        try
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync("Error", "Error fetching overview");
                        }
                        catch { }
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                _logger.LogInformation("Stopped update loop for connection {ConnectionId}", connectionId);
            }
        }

        public async Task RequestOverviewNow()
        {
            if (!_connectionTracker.TryGet(Context.ConnectionId, out var meta))
            {
                await Clients.Caller.SendAsync("Error", "Connection not tracked");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var realTimeService = scope.ServiceProvider.GetRequiredService<IRealTimeService>();

            var overview = await realTimeService.GetOverviewDataAsync(meta.OrganisationId);

            await Clients.Caller.SendAsync("ReceiveOverviewData", overview);
        }
    }
}
