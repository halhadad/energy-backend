namespace energy_backend.Core.Entities
{
    public class Alert
    {
        public Guid AlertId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Alert fires when total org active power (sum of all device
        /// AveragePowerWatts at the latest minute bucket) exceeds this value.
        /// Unit: Watts (W).
        /// </summary>
        public float Threshold { get; set; }

        /// <summary>True while the org power is above Threshold.</summary>
        public bool IsActive { get; set; }

        public DateTime? LastTriggeredAt { get; set; }

        // Navigation properties
        public Organisation? Organisation { get; set; }
    }
}