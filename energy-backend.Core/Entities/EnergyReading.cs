namespace energy_backend.Core.Entities
{
    /// <summary>
    /// Represents a single instantaneous power reading from an IoT device
    /// (e.g. Shelly 3EM). Stored every 5 seconds per device.
    ///
    /// PowerWatts is active power in Watts at the moment of the reading —
    /// NOT accumulated energy. This mirrors exactly what a real power meter
    /// pushes over MQTT.
    /// </summary>
    public class EnergyReading
    {
        public Guid EnergyReadingId { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime Timestamp { get; set; }

        /// <summary>Active power in Watts at this instant.</summary>
        public float PowerWatts { get; set; }

        // Navigation properties
        public Device? Device { get; set; }
    }
}