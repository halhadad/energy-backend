using energy_backend.Application.Models;

namespace energy_backend.Application.Interfaces;

public interface IAlertService
{
    Task<IEnumerable<AlertResponseDto>?> GetActiveAlertsAsync(Guid userId);
    Task<IEnumerable<AlertEventDto>> GetRecentEventsAsync(Guid userId);
    Task<AlertResponseDto?> CreateAlertAsync(Guid userId, AlertRequestDto request);
    Task<bool> ResolveAlertAsync(Guid userId, Guid alertId);
    Task<bool> DeleteAlertAsync(Guid userId, Guid alertId);
}
