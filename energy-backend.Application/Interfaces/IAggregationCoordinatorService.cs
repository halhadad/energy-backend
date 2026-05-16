using System;
using System.Threading.Tasks;
using energy_backend.Core.Entities;

namespace energy_backend.Application.Services
{
    public interface IAggregationCoordinatorService
    {
        /// <summary>
        /// Processes a new EnergyReading, updates minute aggregates, and publishes update events.
        /// </summary>
        /// <param name="energyReading">The new EnergyReading to process.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task ProcessEnergyReadingAsync(EnergyReading energyReading);
    }
}
