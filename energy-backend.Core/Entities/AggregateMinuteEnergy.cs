using System;
using System.ComponentModel.DataAnnotations;

namespace energy_backend.Core.Entities
{
    /// <summary>
    /// Aggregate of all 5-second readings within one minute, per device.
    ///
    /// Power metrics:
    ///   AverageActivePowerWatts — mean active power (W); used for "Current Consumption" card.
    ///   TotalEnergyKwh          — energy consumed in this minute bucket (kWh).
    ///
    /// Electrical quality metrics (averaged over the minute):
    ///   AverageVoltageVolts     — mean RMS voltage; useful for detecting sags/swells.
    ///   AverageCurrentAmps      — mean RMS current.
    ///   AveragePowerFactor      — mean power factor (0–1).
    /// </summary>
    public class AggregateMinuteEnergy
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrgId { get; set; }
        public Guid DeviceId { get; set; }

        /// <summary>UTC start of the one-minute bucket.</summary>
        public DateTime Timestamp { get; set; }

        // ── Energy ────────────────────────────────────────────────────────────

        /// <summary>Total energy consumed in this minute, in kWh.</summary>
        public float TotalEnergyKwh { get; set; }

        // ── Active Power ──────────────────────────────────────────────────────

        /// <summary>Mean active power over this minute, in Watts.</summary>
        public float AverageActivePowerWatts { get; set; }

        /// <summary>Lowest instantaneous active power reading in this minute, in Watts.</summary>
        public float MinActivePowerWatts { get; set; }

        /// <summary>Highest instantaneous active power reading in this minute, in Watts.</summary>
        public float MaxActivePowerWatts { get; set; }

        // ── Electrical metrics ────────────────────────────────────────────────

        /// <summary>Mean RMS voltage over this minute, in Volts.</summary>
        public float AverageVoltageVolts { get; set; }

        /// <summary>Mean RMS current over this minute, in Amperes.</summary>
        public float AverageCurrentAmps { get; set; }

        /// <summary>Mean power factor over this minute (0–1).</summary>
        public float AveragePowerFactor { get; set; }

        // ── Metadata ──────────────────────────────────────────────────────────

        /// <summary>Number of 5-second readings that make up this bucket.</summary>
        public int DataPointsCount { get; set; }

        public Device? Device { get; set; }
    }
}