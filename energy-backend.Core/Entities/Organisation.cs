namespace energy_backend.Entities
{
    public class Organisation
    {
        public Guid OrganisationId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float EnergyBudget { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public ICollection<Device>? Devices { get; set; }

    }
}
