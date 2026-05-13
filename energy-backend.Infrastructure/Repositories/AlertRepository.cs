using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Core.Interfaces;
using energy_backend.Data;
using energy_backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories
{
    public class AlertRepository(EnergyDbContext context): IAlertRepository
    {
        public async Task<IEnumerable<Alert>> GetByUserIdAsync(Guid userId)
        {
            return await context.Alerts
                .Include(a => a.Organisation)
                    .ThenInclude(o => o.Devices)
                .Where(a => a.Organisation!.UserId == userId)
                .ToListAsync();
        }

        public async Task<Alert?> GetByIdAsync(Guid userId, Guid alertId)
        {
            return await context.Alerts
                .Include(a => a.Organisation)
                .FirstOrDefaultAsync(a =>
                    a.AlertId == alertId &&
                    a.Organisation!.UserId == userId);
        }

        public async Task AddAsync(Alert alert)
        {
            await context.Alerts.AddAsync(alert);
        }

        public async Task DeleteAsync(Alert alert)
        {
            context.Alerts.Remove(alert);
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}
