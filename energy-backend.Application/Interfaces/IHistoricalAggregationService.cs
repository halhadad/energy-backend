using System;
using System.Threading.Tasks;

namespace energy_backend.Application.Services
{
    public interface IHistoricalAggregationService
    {
        // Define methods relevant for historical aggregation (e.g., aggregating hourly from minute aggregates)
        Task RunHistoricalAggregationAsync(CancellationToken stoppingToken);
    }
}
