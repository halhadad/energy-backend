using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using energy_backend.Application.Services;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace energy_backend.Infrastructure.Services
{
    public class AlertQueryService : IAlertQueryService
    {
        private readonly EnergyDbContext _context;
        private readonly ILogger<AlertQueryService> _logger;

        public AlertQueryService(EnergyDbContext context, ILogger<AlertQueryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Alert>> GetActiveAlertsForOrganisationAsync(Guid orgId)
        {
            return await _context.Alerts
                .Where(a => a.OrganisationId == orgId && a.IsActive)
                .ToListAsync();
        }

        public async Task<List<Alert>> GetAlertsWithOrganisationsAndDevicesAsync()
        {
            return await _context.Alerts
                .Include(a => a.Organisation)
                    .ThenInclude(o => o!.Devices)
                .ToListAsync();
        }

        public async Task<Alert> GetAlertByIdAsync(Guid alertId)
        {
            return await _context.Alerts.FindAsync(alertId);
        }
    }
}
