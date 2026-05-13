using energy_backend.Core.Entities;
using energy_backend.Hubs;
using energy_backend.Infrastructure.SignalR;
using energy_backend.RealTime.energy_backend.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace energy_backend.RealTime
{
    public class SignalRAlertsNotifier : IAlertsNotifier
    {
        private readonly IHubContext<AlertsHub> _hub;
        private readonly AlertsConnectionTracker _tracker;

        public SignalRAlertsNotifier(
            IHubContext<AlertsHub> hub,
            AlertsConnectionTracker tracker)
        {
            _hub = hub;
            _tracker = tracker;
        }

        public async Task NotifyAsync(Guid alertId, object evt, CancellationToken ct)
        {
            foreach (var conn in _tracker.GetConnectionsForAlert(alertId))
            {
                await _hub.Clients.Client(conn)
                    .SendAsync("AlertTriggered", evt, ct);
            }
        }
    }
}
