using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Application.Models;
using energy_backend.Application.Services;
using energy_backend.Core.Interfaces;
using energy_backend.Entities;
using energy_backend.Infrastructure.Repositories;

namespace energy_backend.Infrastructure.Services
{
    public class AlertsService(
        IAlertRepository alertRepo, IOrganisationRepository orgRepo) : IAlertService
    {
        public async Task<IEnumerable<AlertResponseDto>?> GetActiveAlertsAsync(Guid userId)
        {
            var alerts = await alertRepo.GetByUserIdAsync(userId);

            return alerts
                .Select(a =>
                {
                    var energyConsumption = a.Organisation!.Devices.Sum(d => d.EnergyConsumption);

                    return new
                    {
                        Alert = a,
                        Energy = energyConsumption
                    };
                })
                .Select(x => MapAlertToDto(x.Alert, x.Energy))
                .ToList();
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

            var energyConsumption = organisation.Devices.Sum(d => d.EnergyConsumption);

            return MapAlertToDto(alert, energyConsumption);
        }

        public async Task<bool> DeleteAlertAsync(Guid userId, Guid alertId)
        {
            var alert = await alertRepo.GetByIdAsync(userId, alertId);
            if (alert is null) return false;

            await alertRepo.DeleteAsync(alert);
            await alertRepo.SaveChangesAsync();

            return true;
        }

        private static AlertResponseDto MapAlertToDto(Alert alert, float energyConsumption) => new()
        {
            AlertId = alert.AlertId,
            Name = alert.Name,
            Threshold = alert.Threshold,
            EnergyConsumption = energyConsumption,
            IsActive = alert.IsActive,
            LastTriggeredAt = alert.LastTriggeredAt
        };
    }
}

