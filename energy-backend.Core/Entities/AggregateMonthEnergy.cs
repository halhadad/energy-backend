using System;
using System.ComponentModel.DataAnnotations;

namespace energy_backend.Core.Entities
{
    /// <summary>
    /// Aggregate of all day buckets within one calendar month, per device.
    /// </summary>
    public class AggregateMonthEnergy
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrgId { get; set; }
        public Guid DeviceId { get; set; }

        /// <summary>UTC first-of-month start of the month bucket.</summary>
        public DateTime Timestamp { get; set; }

        public float TotalEnergyKwh { get; set; }

        public float AverageActivePowerWatts { get; set; }
        public float MinActivePowerWatts { get; set; }
        public float MaxActivePowerWatts { get; set; }

        public float AverageVoltageVolts { get; set; }
        public float AverageCurrentAmps { get; set; }
        public float AveragePowerFactor { get; set; }

        public int DataPointsCount { get; set; }

        public Device? Device { get; set; }
    }
}