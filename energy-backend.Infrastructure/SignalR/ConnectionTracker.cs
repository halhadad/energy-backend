using System;
using System.Collections.Generic;
using System.Linq;

namespace energy_backend.Infrastructure.SignalR
{
    // Lightweight in-memory tracker for active SignalR connections
    // Holds mapping: ConnectionId -> (UserId, OrganisationId)
    // Registered as a singleton in DI.
    public class ConnectionTracker
    {
        private readonly Dictionary<string, (Guid UserId, Guid OrganisationId)> _connections
            = new Dictionary<string, (Guid, Guid)>();

        private readonly object _lock = new object();

        public void Add(string connectionId, Guid userId, Guid organisationId)
        {
            lock (_lock)
            {
                _connections[connectionId] = (userId, organisationId);
            }
        }

        public void Remove(string connectionId)
        {
            lock (_lock)
            {
                _connections.Remove(connectionId);
            }
        }

        public bool TryGet(string connectionId, out (Guid UserId, Guid OrganisationId) value)
        {
            lock (_lock)
            {
                if (_connections.TryGetValue(connectionId, out var v))
                {
                    value = v;
                    return true;
                }

                value = default;
                return false;
            }
        }

        public List<Guid> GetActiveOrganisations()
        {
            lock (_lock)
            {
                return _connections.Values.Select(v => v.OrganisationId).Distinct().ToList();
            }
        }

        public List<string> GetConnectionsForOrganisation(Guid orgId)
        {
            lock (_lock)
            {
                return _connections.Where(kv => kv.Value.OrganisationId == orgId)
                    .Select(kv => kv.Key).ToList();
            }
        }
    }
}