using energy_backend.Core.Entities;

namespace energy_backend.Core.Interfaces;

public interface IEnergyRateRepository
{
    Task<List<EnergyRate>> GetRatesInRangeAsync(Guid orgId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task<decimal> GetCurrentRateAsync(Guid orgId, CancellationToken ct = default);
    Task<bool> HasAnyAsync(Guid orgId, CancellationToken ct = default);

    Task AddAsync(EnergyRate rate, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}