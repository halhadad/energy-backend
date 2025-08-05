namespace energy_backend.Entities
{
    public class Device
    {
        public Guid DeviceId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float EnergyConsumption { get; set; }
        // history stuff to be dealth with later

        // Navigation properties
        public Organisation? Organisation { get; set; } // many to one relationship
    }
}
