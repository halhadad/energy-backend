// MOVE: energy-backend.Core/Interfaces/IEnergyRollupRepository.cs
//    → energy-backend.Application/Interfaces/IEnergyRollupRepository.cs

namespace energy_backend.Application.Interfaces;  // was Core.Interfaces

using energy_backend.Application.Models.Projections;  // now legal — same layer

public interface IEnergyRollupRepository
{
    Task<List<MinuteRollupProjection>> GetRawMinuteRollupsAsync(
        DateTime since, CancellationToken ct = default);
}