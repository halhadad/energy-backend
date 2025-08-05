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
    public class OrganisationRepository(EnergyDbContext context) : IOrganisationRepository
    {
        public async Task<IEnumerable<Organisation>> GetAllByUserIdAsync(Guid userId)
        {
            return await context.Organisations
                .Where(o => o.UserId == userId)
                .Include(o => o.Devices)
                .ToListAsync();
        }

        public async Task<Organisation?> GetByIdAsync(Guid userId, Guid organisationId)
        {
            return await context.Organisations
                .Include(o => o.Devices)
                .FirstOrDefaultAsync(o => o.OrganisationId == organisationId && o.UserId == userId);
        }

        public async Task<Organisation?> AddAsync(Organisation organisation)
        {
            await context.Organisations.AddAsync(organisation);
            return organisation;
        }

        public async Task<bool> DeleteAsync(Organisation organisation)
        {
            context.Organisations.Remove(organisation);
            return true;
        }

        public async Task<bool> ExistsAsync(Guid userId)
        {
            return await context.Organisations.AnyAsync(o => o.UserId == userId);
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}
