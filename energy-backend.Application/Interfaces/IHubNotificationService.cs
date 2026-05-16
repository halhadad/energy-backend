using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using energy_backend.Core.Entities;
using energy_backend.Application.Models.SignalR;


namespace energy_backend.Application.Services
{
    // Abstraction for sending hub notifications to maintain clean architecture
    public interface IHubNotificationService
    {
        // Chart Streaming
        Task SendRealTimeChartUpdateAsync(string groupName, RealTimeChartDataDto data);
        Task SendRealTimeChartCatchUpAsync(string connectionId, RealTimeChartDataDto data);
        Task AddToChartGroupAsync(string connectionId, string groupName);
        Task RemoveFromChartGroupAsync(string connectionId, string groupName);

        // Alert Streaming
        Task SendAlertTriggeredAsync(string groupName, AlertEvent alertEvent);
        Task SendAlertResolvedAsync(string groupName, Alert alert);
        Task SendAlertUpdatedAsync(string groupName, Alert alert);
        Task AddToAlertGroupAsync(string connectionId, string groupName);
        Task RemoveFromAlertGroupAsync(string connectionId, string groupName);

        // Device Streaming (Placeholder)
        Task AddToDeviceGroupAsync(string connectionId, string groupName);
        Task RemoveFromDeviceGroupAsync(string connectionId, string groupName);

        // General
        Task SendErrorAsync(string connectionId, string message);
    }
}
