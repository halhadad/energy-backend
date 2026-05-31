namespace energy_backend.Core.Services;

public static class EnergyCalculator
{
    private const float JoulesPerKwh = 3_600_000f;

    public static float CalculateKwhFromWatts(float watts, float intervalSeconds)
    {
        return (watts * intervalSeconds) / JoulesPerKwh;
    }

    public static float CalculateEstimatedCost(float kwh, float costRate)
    {
        return (float)Math.Round(kwh * costRate, 4);
    }
}