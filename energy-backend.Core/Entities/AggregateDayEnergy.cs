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

        /// <summary>Total energy consumed in this day, in kWh.</summary>
        public float TotalEnergyKwh { get; set; }

        /// <summary>Mean active power over this day, in Watts.</summary>
        public float AveragePowerWatts { get; set; }

        /// <summary>Lowest instantaneous reading in this day, in Watts.</summary>
        public float MinPowerWatts { get; set; }

        /// <summary>Highest instantaneous reading in this day, in Watts.</summary>
        public float MaxPowerWatts { get; set; }

        public int DataPointsCount { get; set; }

        public Device? Device { get; set; }
    }
}