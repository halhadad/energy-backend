namespace energy_backend.Application.Configuration;

public sealed class AuthOptions
{
    public const string Section = "Auth";

    public int AccessTokenLifetimeHours { get; set; } = 24;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
