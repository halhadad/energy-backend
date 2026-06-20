namespace energy_backend.Core.Services;

public static class EnergyCalculator
{
    private const double JoulesPerKwh = 3_600_000d;

    public static double CalculateKwhFromWatts(double watts, double intervalSeconds)
    {
        return (watts * intervalSeconds) / JoulesPerKwh;
    }

    public static decimal CalculateEstimatedCost(double kwh, decimal ratePerKwh)
    {
        return (decimal)kwh * ratePerKwh;
    }
}
