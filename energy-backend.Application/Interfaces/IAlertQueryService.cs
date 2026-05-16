using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using energy_backend.Core.Entities;

namespace energy_backend.Application.Services
{
    // Abstraction for querying alert-related data from infrastructure
    public interface IAlertQueryService
    {
        // Example: Get all active alerts for an organization
        Task<List<Alert>> GetActiveAlertsForOrganisationAsync(Guid orgId);

        // This is for the AlertsMonitorService - will need to be refactored
        // It should provide the data needed for AlertsMonitorService to evaluate alerts
        Task<List<Alert>> GetAlertsWithOrganisationsAndDevicesAsync();
        Task<Alert> GetAlertByIdAsync(Guid alertId);
    }
}
