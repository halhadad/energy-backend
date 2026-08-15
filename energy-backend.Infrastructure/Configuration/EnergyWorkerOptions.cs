namespace energy_backend.Infrastructure.Configuration;

public sealed class EnergyWorkerOptions
{
    public const string Section = "EnergyWorkers";

    public int SimulatorIntervalSeconds { get; set; } = 5;
    public int BroadcastIntervalSeconds { get; set; } = 5;
    public double NominalVoltage { get; set; } = 230d;
    public int DownsamplerLookbackHours { get; set; } = 2;
    public double AlertHysteresisRatio { get; set; } = 0.9;
    public int AlertEvaluationIntervalSeconds { get; set; } = 5;
}
