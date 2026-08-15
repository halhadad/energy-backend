namespace energy_backend.Application.Models;

public class SettingResponseDto
{
    public string Email { get; set; } = string.Empty;
    public bool RequireEmail { get; set; }
}

public class UpdateSettingDto
{
    public string? Email { get; set; }
    public bool? RequireEmail { get; set; }
}
