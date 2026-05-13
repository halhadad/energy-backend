namespace energy_backend.Entities
{
    public class Alert
    {
        public Guid AlertId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public float Threshold { get; set; }

        // State
        public bool IsActive { get; set; }          // prevents retrigger spam
        public DateTime? LastTriggeredAt { get; set; }

        // Navigation properties
        public Organisation? Organisation { get; set; }
    }
}
