using energy_backend.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace energy_backend.Core.Entities
{
    public class AggregateDayEnergy : IEnergyAggregate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrgId { get; set; }
        public Guid? DeviceId { get; set; }
        public DateTime Timestamp { get; set; } // UTC start of the day
        public double TotalActiveEnergyKwh { get; set; }
        public double AverageActivePowerWatts { get; set; }
        public double MinActivePowerWatts { get; set; }
        public double MaxActivePowerWatts { get; set; }
        public double AverageVoltageVolts { get; set; }
        public double AverageCurrentAmps { get; set; }
        public double AveragePowerFactor { get; set; }
        public int DataPointsCount { get; set; }

        public Device? Device { get; set; }
    }
}
