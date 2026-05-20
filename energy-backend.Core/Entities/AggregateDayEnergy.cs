using System;
using System.ComponentModel.DataAnnotations;

namespace energy_backend.Core.Entities
{
    /// <summary>
    /// Aggregate of all hour buckets within one calendar day, per device.
    /// </summary>
    public class AggregateDayEnergy
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrgId { get; set; }
        public Guid DeviceId { get; set; }

        /// <summary>UTC midnight start of the day bucket.</summary>
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