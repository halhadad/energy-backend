using energy_backend.Core.Entities;

namespace energy_backend.Application.Services
{
    public interface IAggregationCoordinatorService
    {
        Task ProcessEnergyReadingAsync(EnergyReading energyReading);
    }
}