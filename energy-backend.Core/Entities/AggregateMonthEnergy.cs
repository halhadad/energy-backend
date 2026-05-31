using energy_backend.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace energy_backend.Core.Entities
{

    public class AggregateMonthEnergy : IEnergyAggregate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrgId { get; set; }
        public Guid? DeviceId { get; set; }
        public DateTime Timestamp { get; set; } // UTC start of the month (e.g. 2024-01-01T00:00:00Z)
        public float TotalActiveEnergyKwh { get; set; }
        public float AverageActivePowerWatts { get; set; }
        public float MinActivePowerWatts { get; set; }
        public float MaxActivePowerWatts { get; set; }
        public float AverageVoltageVolts { get; set; }
        public float AverageCurrentAmps { get; set; }
        public float AveragePowerFactor { get; set; }
        public int DataPointsCount { get; set; }
        public float EstimatedCost { get; set; }


        // Navigation property
        public Device? Device { get; set; }
    }
}