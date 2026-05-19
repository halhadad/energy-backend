namespace energy_backend.Models
{
    public class DeviceRequestDto
    {
        public Guid OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        /// <summary>Nameplate / rated power in Watts.</summary>
        public float RatedPowerWatts { get; set; }
    }
}