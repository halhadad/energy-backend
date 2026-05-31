using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;

namespace energy_backend.Application.Services;

public class AlertService(
    IAlertRepository alertRepo,
    IOrganisationRepository orgRepo) : IAlertService
{
    public async Task<IEnumerable<AlertResponseDto>?> GetActiveAlertsAsync(Guid userId)
    {
        var alerts = await alertRepo.GetByUserIdAsync(userId);
        return alerts.Select(MapAlertToDto).ToList();
    }

    public async Task<AlertResponseDto?> CreateAlertAsync(Guid userId, AlertRequestDto request)
    {
        var organisation = await orgRepo.GetByIdAsync(userId, request.OrganisationId);
        if (organisation is null) return null;

        var alert = new Alert
        {
            AlertId = Guid.NewGuid(),
            OrganisationId = request.OrganisationId,
            Name = request.Name,
            ThresholdValue = request.Threshold
        };

        await alertRepo.AddAsync(alert);
        await alertRepo.SaveChangesAsync();

        return MapAlertToDto(alert);
    }

    public async Task<bool> DeleteAlertAsync(Guid userId, Guid alertId)
    {
        var alert = await alertRepo.GetByIdAsync(userId, alertId);
        if (alert is null) return false;

        await alertRepo.DeleteAsync(alert);
        await alertRepo.SaveChangesAsync();
        return true;
    }

    private static AlertResponseDto MapAlertToDto(Alert alert) => new()
    {
        AlertId = alert.AlertId,
        Name = alert.Name,
        Threshold = alert.ThresholdValue,
        IsActive = alert.IsActive,
        LastTriggeredAt = alert.LastTriggeredInAppAt
    };
}
