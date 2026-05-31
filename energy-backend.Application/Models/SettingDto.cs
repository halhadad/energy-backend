// energy-backend.Application/Models/SettingDto.cs
namespace energy_backend.Application.Models;

public class SettingResponseDto
{
    public bool RequireEmail { get; set; }
}

public class UpdateSettingDto
{
    public bool? RequireEmail { get; set; }
}
