namespace energy_backend.Entities
{
    public class Alert
    {
        public Guid AlertId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public float Threshold { get; set; }

        // Navigation properties
        public Organisation? Organisation { get; set; }
    }
}
