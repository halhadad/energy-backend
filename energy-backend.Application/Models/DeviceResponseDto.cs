namespace energy_backend.Models
{
    public class DeviceResponseDto
    {
        public Guid DeviceId { get; set; }
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float EnergyConsumption { get; set; }
    }
}