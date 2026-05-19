using System;
using System.ComponentModel.DataAnnotations;

namespace energy_backend.Core.Entities
{
    /// <summary>
    /// Aggregate of all minute buckets within one hour, per device.
    /// </summary>
    public class AggregateHourEnergy
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrgId { get; set; }
        public Guid DeviceId { get; set; }

        /// <summary>UTC start of the one-hour bucket.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Total energy consumed in this hour, in kWh.</summary>
        public float TotalEnergyKwh { get; set; }

        /// <summary>Mean active power over this hour, in Watts.</summary>
        public float AveragePowerWatts { get; set; }

        /// <summary>Lowest instantaneous reading in this hour, in Watts.</summary>
        public float MinPowerWatts { get; set; }

        /// <summary>Highest instantaneous reading in this hour, in Watts.</summary>
        public float MaxPowerWatts { get; set; }

        public int DataPointsCount { get; set; }

        public Device? Device { get; set; }
    }
}