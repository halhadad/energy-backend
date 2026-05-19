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

        /// <summary>Total energy consumed in this month, in kWh.</summary>
        public float TotalEnergyKwh { get; set; }

        /// <summary>Mean active power over this month, in Watts.</summary>
        public float AveragePowerWatts { get; set; }

        /// <summary>Lowest instantaneous reading in this month, in Watts.</summary>
        public float MinPowerWatts { get; set; }

        /// <summary>Highest instantaneous reading in this month, in Watts.</summary>
        public float MaxPowerWatts { get; set; }

        public int DataPointsCount { get; set; }

        public Device? Device { get; set; }
    }
}