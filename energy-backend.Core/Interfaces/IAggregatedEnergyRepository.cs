using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace energy_backend.Core.Interfaces
{
    public interface IAggregatedEnergyRepository
    {
        Task<Dictionary<string, float>> GetAggregatedByDeviceTypeAsync(Guid organisationId, DateTime start, DateTime end);
        Task<Dictionary<string, float>> GetAggregatedByHourAsync(Guid organisationId, DateTime start, DateTime end);
        Task<Dictionary<string, float>> GetAggregatedByDayAsync(Guid organisationId, DateTime start, DateTime end);
        Task<float> GetTotalConsumptionAsync(Guid organisationId, DateTime start, DateTime end);
    }
}
