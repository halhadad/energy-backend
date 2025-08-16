using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Entities;

namespace energy_backend.Core.Interfaces
{
    public interface IEnergyReadingRepository
    {
        Task<List<EnergyReading>> GetReadingsForTodayAsync(Guid organisationId);
        Task<List<EnergyReading>> GetReadingsForWeekAsync(Guid organisationId);
        Task<float> GetCurrentConsumptionAsync(Guid organisationId, DateTime since);
        Task<float> GetTodaysCostAsync(Guid organisationId, float costPerUnit);
        Task<Dictionary<string, float>> GetDailyBreakdownByDeviceTypeAsync(Guid organisationId);
        Task<Dictionary<string, float>> GetWeeklyBreakdownByDeviceTypeAsync(Guid organisationId);
        Task<Dictionary<string, float>> GetHourlyBreakdownTodayAsync(Guid organisationId);
        Task<Dictionary<string, float>> GetDailyBreakdownThisWeekAsync(Guid organisationId);
    }
}
