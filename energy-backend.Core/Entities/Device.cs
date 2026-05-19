namespace energy_backend.Core.Entities
{
    /// <summary>
    /// A physical IoT-monitored device belonging to an Organisation.
    /// RatedPowerWatts is the nameplate power (W) used as the simulation
    /// baseline and the budget reference shown on the dashboard.
    /// </summary>
    public class Device
    {
        public Guid DeviceId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Nameplate / rated power in Watts.
        /// Used as the simulation baseline and shown as the "rated" midpoint
        /// on the Current Consumption progress bar.
        /// </summary>
        public float RatedPowerWatts { get; set; }

        // Navigation properties
        public Organisation? Organisation { get; set; }
    }
}