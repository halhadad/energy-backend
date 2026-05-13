using System;
using System.Collections.Generic;
using System.Linq;

namespace energy_backend.Infrastructure.SignalR
{
    // Lightweight in-memory tracker for active SignalR connections
    // Holds mapping: ConnectionId -> (UserId, AlertId)
    // Registered as a singleton in DI.
    public class AlertsConnectionTracker
    {
        // Maps ConnectionId -> List of (UserId, AlertId)
        private readonly Dictionary<string, List<(Guid UserId, Guid AlertId)>> _connections = new();
        private readonly object _lock = new();

        public void Add(string connectionId, Guid userId, Guid alertId)
        {
            lock (_lock)
            {
                if (!_connections.ContainsKey(connectionId))
                {
                    _connections[connectionId] = new List<(Guid UserId, Guid AlertId)>();
                }
                
                // Prevent duplicate subscriptions for the same alert on the same connection
                if (!_connections[connectionId].Any(x => x.AlertId == alertId))
                {
                    _connections[connectionId].Add((userId, alertId));
                }
            }
        }

        public void Remove(string connectionId)
        {
            lock (_lock)
            {
                _connections.Remove(connectionId);
            }
        }

        public void RemoveAlert(string connectionId, Guid alertId)
        {
            lock (_lock)
            {
                if (_connections.TryGetValue(connectionId, out var subs))
                {
                    subs.RemoveAll(x => x.AlertId == alertId);
                }
            }
        }

        public List<string> GetConnectionsForAlert(Guid alertId)
        {
            lock (_lock)
            {
                return _connections
                    .Where(kvp => kvp.Value.Any(sub => sub.AlertId == alertId))
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
        }
    }

}