using System;
using System.ComponentModel.DataAnnotations;

namespace energy_backend.Core.Entities
{
    /// <summary>
    /// Aggregate of all 5-second power readings within one minute, per device.
    /// TotalEnergyKwh = energy consumed in that minute (kWh).
    /// AveragePowerWatts = mean active power over the minute (W) — this is
    /// what the "Current Consumption" card reads from the latest bucket.
    /// </summary>
    public class AggregateMinuteEnergy
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrgId { get; set; }
        public Guid DeviceId { get; set; }

        /// <summary>UTC start of the one-minute bucket.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Total energy consumed in this minute, in kWh.</summary>
        public float TotalEnergyKwh { get; set; }

        /// <summary>Mean active power over this minute, in Watts.</summary>
        public float AveragePowerWatts { get; set; }

        /// <summary>Lowest instantaneous reading in this minute, in Watts.</summary>
        public float MinPowerWatts { get; set; }

        /// <summary>Highest instantaneous reading in this minute, in Watts.</summary>
        public float MaxPowerWatts { get; set; }

        /// <summary>Number of 5-second readings that make up this bucket.</summary>
        public int DataPointsCount { get; set; }

        public Device? Device { get; set; }
    }
}