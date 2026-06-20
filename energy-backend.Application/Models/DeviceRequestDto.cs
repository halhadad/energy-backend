namespace energy_backend.Application.Models;

public class DeviceRequestDto
{
    public Guid OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double RatedPowerWatts { get; set; }
}
