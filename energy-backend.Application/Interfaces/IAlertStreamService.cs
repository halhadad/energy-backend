using System;
using System.Threading.Tasks;
using energy_backend.Core.Entities;

namespace energy_backend.Application.Services
{
    public interface IAlertStreamService // Renamed
    {
        Task SubscribeToAlerts(string connectionId, Guid orgId);
        Task UnsubscribeFromAlerts(string connectionId, Guid orgId);

        // Methods for event-driven updates (to be called by AlertsMonitorService)
        Task NotifyAlertTriggered(Guid orgId, AlertEvent alertEvent);
        Task NotifyAlertResolved(Guid orgId, Alert alert); // When an alert moves from active to resolved/inactive
        Task NotifyAlertUpdated(Guid orgId, Alert alert); // For any other significant alert state change
    }
}
