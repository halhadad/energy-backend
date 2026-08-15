using energy_backend.Core.Entities;

namespace energy_backend.Core.Interfaces
{
    public interface IEnergyReadingRepository
    {
        Task<List<EnergyReading>> GetReadingsSinceAsync(Guid organisationId, DateTime since);
    }
}
