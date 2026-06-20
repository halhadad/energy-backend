namespace energy_backend.Application.Configuration;

public sealed class EnergySettings
{
    public const string Section = "EnergySettings";

    public decimal CostPerKwh { get; set; } = 0.28m;
    public int LiveWindowMinutes { get; set; } = 30;
}
