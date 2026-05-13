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
using System.Security.Cryptography.Xml;
using energy_backend.Data;

namespace energy_backend.Hubs
{
    [Authorize]
    public class AlertsHub : Hub
    {
        private readonly AlertsConnectionTracker _tracker;
        private readonly EnergyDbContext _dbContext;

        public AlertsHub(AlertsConnectionTracker tracker, EnergyDbContext dbContext)
        {
            _tracker = tracker;
            _dbContext = dbContext;
        }

        public async Task Subscribe(Guid alertId)
        {
            var userId = GetUserId();
            _tracker.Add(Context.ConnectionId, userId, alertId);

            // Fetch the alert to see if it's already active.
            // If it triggered while the client was connecting, they missed the broadcast.
            var alert = await _dbContext.Alerts.FindAsync(alertId);
            if (alert != null && alert.IsActive)
            {
                // Send an immediate catch-up notification to this specific client
                var evtDto = new
                {
                    AlertEventId = Guid.NewGuid(),
                    AlertId = alert.AlertId,
                    OrganisationId = alert.OrganisationId,
                    Name = alert.Name,
                    Threshold = alert.Threshold,
                    TriggeredEnergy = alert.Threshold + 1, // Simulated catch-up value
                    TriggeredAt = alert.LastTriggeredAt ?? DateTime.UtcNow
                };

                await Clients.Caller.SendAsync("AlertTriggered", evtDto);
            }
        }

        public Task Unsubscribe(Guid alertId)
        {
            _tracker.RemoveAlert(Context.ConnectionId, alertId);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _tracker.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        private Guid GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new HubException("Missing user claim");

            return Guid.Parse(claim);
        }
    }

}
