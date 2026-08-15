using energy_backend.Application.Models.SignalR;
using energy_backend.Core.Entities;

namespace energy_backend.Application.Interfaces
{
    public interface IHubNotificationService
    {
        // Chart group (aggregate rollups)
        Task SendRealTimeChartUpdateAsync(string groupName, RealTimeEnergyIntervalDto data);
        Task AddToChartGroupAsync(string connectionId, string groupName);
        Task RemoveFromChartGroupAsync(string connectionId, string groupName);

        // Live tick group (raw readings)
        Task SendLiveTickAsync(string groupName, LiveTickDto tick);
        Task AddToLiveGroupAsync(string connectionId, string groupName);
        Task RemoveFromLiveGroupAsync(string connectionId, string groupName);

        // Historical "data changed" signal group
        Task AddToHistoricalGroupAsync(string connectionId, string groupName);
        Task RemoveFromHistoricalGroupAsync(string connectionId, string groupName);
        Task SendHistoricalUpdateAsync(string groupName, object payload);

        // Alert group
        Task SendAlertTriggeredAsync(string groupName, AlertEvent alertEvent);
        Task SendAlertResolvedAsync(string groupName, Alert alert);
        Task SendAlertUpdatedAsync(string groupName, Alert alert);
        Task AddToAlertGroupAsync(string connectionId, string groupName);
        Task RemoveFromAlertGroupAsync(string connectionId, string groupName);

        Task SendErrorAsync(string connectionId, string message);
    }
}
