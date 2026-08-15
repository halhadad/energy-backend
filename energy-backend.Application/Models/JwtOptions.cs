namespace energy_backend.Application.Models;

public class JwtOptions
{
    public string Token { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}