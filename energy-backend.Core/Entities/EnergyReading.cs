namespace energy_backend.Core.Entities
{
    /// <summary>
    /// A single instantaneous reading from an IoT power meter (e.g. Shelly 3EM).
    /// Stored every 5 seconds per device.
    ///
    /// Mirrors a real single-phase power meter payload:
    ///   ActivePowerWatts  = P  (W)   — real power doing actual work
    ///   VoltageVolts      = V  (V)   — RMS line voltage (~230 V EU / ~120 V US)
    ///   CurrentAmps       = I  (A)   — RMS current draw
    ///   PowerFactor       = PF (0–1) — cos φ; 1.0 = purely resistive load
    ///
    /// Relationship: P = V × I × PF  (active power formula)
    /// </summary>
    public class EnergyReading
    {
        public Guid EnergyReadingId { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime Timestamp { get; set; }

        /// <summary>Active (real) power in Watts at this instant.</summary>
        public float ActivePowerWatts { get; set; }

        /// <summary>RMS line voltage in Volts (e.g. ~230 V for EU grid).</summary>
        public float VoltageVolts { get; set; }

        /// <summary>RMS current draw in Amperes.</summary>
        public float CurrentAmps { get; set; }

        /// <summary>
        /// Power factor (0.0–1.0). Resistive loads (heaters, incandescent bulbs)
        /// are close to 1.0; motors and SMPS devices are typically 0.70–0.95.
        /// </summary>
        public float PowerFactor { get; set; }

        // Navigation properties
        public Device? Device { get; set; }
    }
}