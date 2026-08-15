
using energy_backend.Application.Configuration;
using energy_backend.Core.Entities;
using energy_backend.Core.Interfaces;
using energy_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Repositories;

public class EnergyRateRepository(EnergyDbContext context, EnergySettings settings) : IEnergyRateRepository
{
    private decimal Fallback => settings.CostPerKwh;

    public async Task<List<EnergyRate>> GetRatesInRangeAsync(
        Guid orgId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var s = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        var e = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);
        return await context.EnergyRates
            .Where(r => r.OrganisationId == orgId
                     && r.ValidFromUtc < e
                     && (r.ValidToUtc == null || r.ValidToUtc > s))
            .OrderBy(r => r.ValidFromUtc)
            .ToListAsync(ct);
    }

    public async Task<decimal> GetCurrentRateAsync(Guid orgId, CancellationToken ct = default)
    {
        var open = await context.EnergyRates
            .Where(r => r.OrganisationId == orgId && r.ValidToUtc == null)
            .OrderByDescending(r => r.ValidFromUtc)
            .FirstOrDefaultAsync(ct);
        if (open is not null) return open.RatePerKwh;

        // Fallback: closed rate that covers right now
        var now = DateTime.UtcNow;
        var closed = await context.EnergyRates
            .Where(r => r.OrganisationId == orgId
                     && r.ValidFromUtc <= now
                     && r.ValidToUtc > now)
            .OrderByDescending(r => r.ValidFromUtc)
            .FirstOrDefaultAsync(ct);
        return closed?.RatePerKwh ?? Fallback;
    }

    public async Task<bool> HasAnyAsync(Guid orgId, CancellationToken ct = default)
        => await context.EnergyRates.AnyAsync(r => r.OrganisationId == orgId, ct);

    public async Task AddAsync(EnergyRate rate, CancellationToken ct = default)
        => await context.EnergyRates.AddAsync(rate, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}