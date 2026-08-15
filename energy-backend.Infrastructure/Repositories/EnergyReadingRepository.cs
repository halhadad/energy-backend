using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories
{
    public class EnergyReadingRepository(EnergyDbContext context) : IEnergyReadingRepository
    {
        public async Task<List<EnergyReading>> GetReadingsSinceAsync(
            Guid organisationId, DateTime since)
        {
            var sinceUtc = DateTime.SpecifyKind(since, DateTimeKind.Utc);
            return await context.EnergyReadings
                .Where(r => r.OrgId == organisationId && r.Timestamp >= sinceUtc)
                .OrderBy(r => r.Timestamp)
                .ToListAsync();
        }
    }
}
