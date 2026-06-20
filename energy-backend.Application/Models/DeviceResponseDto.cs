namespace energy_backend.Application.Models;

public class DeviceResponseDto
{
    public Guid DeviceId { get; set; }
    public Guid OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double RatedPowerWatts { get; set; }
}
