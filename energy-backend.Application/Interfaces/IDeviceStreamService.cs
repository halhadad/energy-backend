using System;
using System.Threading.Tasks;
using energy_backend.Core.Entities; // For Device (if needed for method signatures)
using energy_backend.Application.Models.SignalR; // For consistency

namespace energy_backend.Application.Services
{
    public interface IDeviceStreamService
    {
        Task SubscribeToDevices(string connectionId, Guid orgId);
        Task UnsubscribeFromDevices(string connectionId, Guid orgId);
        // Add methods for device-specific updates if needed later
    }
}
