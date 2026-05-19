using energy_backend.Application.Models;
using energy_backend.Application.Services;
using energy_backend.Core.Interfaces;
using energy_backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using energy_backend.Data;

namespace energy_backend.Infrastructure.Services
{
    public class AlertsService(
        IAlertRepository alertRepo,
        IOrganisationRepository orgRepo,
        EnergyDbContext context) : IAlertService
    {
        public async Task<IEnumerable<AlertResponseDto>?> GetActiveAlertsAsync(Guid userId)
        {
            var alerts = await alertRepo.GetByUserIdAsync(userId);
            return alerts.Select(a =>
            {
                var currentPower = a.Organisation!.Devices.Any() 
                    ? a.Organisation.Devices.Count > 0 ? 0f : 0f 
                    : 0f;
                return MapAlertToDto(a, currentPower);
            }).ToList();
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
                Threshold = request.Threshold
            };

            await alertRepo.AddAsync(alert);
            await alertRepo.SaveChangesAsync();

            var currentPower = organisation.Devices?.Any() == true ? 0f : 0f;
            return MapAlertToDto(alert, currentPower);
        }

        public async Task<bool> DeleteAlertAsync(Guid userId, Guid alertId)
        {
            var alert = await alertRepo.GetByIdAsync(userId, alertId);
            if (alert is null) return false;
            await alertRepo.DeleteAsync(alert);
            await alertRepo.SaveChangesAsync();
            return true;
        }

        public async Task<List<Alert>> GetAlertsWithOrganisationsAsync()
        {
            return await context.Alerts
                .Include(a => a.Organisation)
                .ToListAsync();
        }

        private static AlertResponseDto MapAlertToDto(Alert alert, float currentPower) => new()
        {
            AlertId = alert.AlertId,
            Name = alert.Name,
            Threshold = alert.Threshold,
            CurrentPowerWatts = currentPower,
            IsActive = alert.IsActive,
            LastTriggeredAt = alert.LastTriggeredAt
        };
    }
}