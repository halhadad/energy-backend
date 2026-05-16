using energy_backend.Application.Models;
using energy_backend.Core.Entities;
using energy_backend.Models;

namespace energy_backend.Application.Services
{
    public interface IAlertService
    {
        Task<IEnumerable<AlertResponseDto>?> GetActiveAlertsAsync(Guid userId);
        Task<AlertResponseDto?> CreateAlertAsync(Guid userId, AlertRequestDto request);
        Task<bool> DeleteAlertAsync(Guid userId, Guid alertId);
        // Used internally by AlertsMonitorService — no separate IAlertQueryService needed
        Task<List<Alert>> GetAlertsWithOrganisationsAsync();
    }
}