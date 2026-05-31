using energy_backend.Core.Entities;

namespace energy_backend.Application.Interfaces;

public interface IAlertStreamService
{
    Task SubscribeToAlerts(string connectionId, Guid orgId);
    Task UnsubscribeFromAlerts(string connectionId, Guid orgId);
    Task NotifyAlertTriggered(Guid orgId, AlertEvent alertEvent);
    Task NotifyAlertResolved(Guid orgId, Alert alert);
    Task NotifyAlertUpdated(Guid orgId, Alert alert);
}
