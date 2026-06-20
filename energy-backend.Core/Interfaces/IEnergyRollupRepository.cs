using energy_backend.Core.Projections;

namespace energy_backend.Core.Interfaces;

public interface IEnergyRollupRepository
{
    Task<List<MinuteRollupProjection>> GetRawMinuteRollupsAsync(
        DateTime since, CancellationToken ct = default);
}
