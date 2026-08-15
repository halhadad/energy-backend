using energy_backend.Application.Interfaces;
using energy_backend.Application.Models;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;

namespace energy_backend.Application.Services;

public class AlertService(
    IAlertRepository alertRepo,
    IOrganisationRepository orgRepo,
    IAlertStreamService alertStream) : IAlertService
{
    public async Task<IEnumerable<AlertResponseDto>?> GetActiveAlertsAsync(Guid userId)
    {
        var alerts = await alertRepo.GetByUserIdAsync(userId);
        return alerts.Select(MapAlertToDto).ToList();
    }

    public async Task<IEnumerable<AlertEventDto>> GetRecentEventsAsync(Guid userId)
    {
        var events = await alertRepo.GetRecentEventsByUserIdAsync(userId);
        return events.Select(e => new AlertEventDto
        {
            AlertEventId = e.AlertEventId,
            AlertId = e.AlertId,
            Name = e.Name,
            ThresholdValue = e.ThresholdValue,
            TriggeredValueWatts = e.TriggeredValueWatts,
            TriggeredAt = DateTime.SpecifyKind(e.TriggeredAt, DateTimeKind.Utc)
        });
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
            ThresholdValue = request.Threshold,
            EmailEnabled = request.EmailEnabled,
            InAppNotificationEnabled = request.InAppEnabled
        };

        await alertRepo.AddAsync(alert);
        await alertRepo.SaveChangesAsync();

        return MapAlertToDto(alert);
    }

    public async Task<bool> ResolveAlertAsync(Guid userId, Guid alertId)
    {
        var alert = await alertRepo.GetByIdAsync(userId, alertId);
        if (alert is null) return false;
        if (alert.ResolvedAt is not null) return true;

        // Resolve is terminal: mark it resolved (history) so the evaluator no longer
        // re-triggers it, even while power is still above the threshold.
        alert.IsActive = false;
        alert.ResolvedAt = DateTime.UtcNow;
        await alertRepo.SaveChangesAsync();
        await alertStream.NotifyAlertResolved(alert.OrganisationId, alert);
        return true;
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
        LastTriggeredAt = alert.LastTriggeredInAppAt,
        ResolvedAt = alert.ResolvedAt,
        Status = alert.IsActive ? "Triggered" : alert.ResolvedAt is not null ? "Resolved" : "Monitoring",
        EmailEnabled = alert.EmailEnabled,
        InAppEnabled = alert.InAppNotificationEnabled
    };
}
