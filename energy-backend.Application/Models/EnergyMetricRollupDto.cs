namespace energy_backend.Application.Models;

public class EnergyMetricRollupDto
{
    public DateTime Timestamp { get; set; }
    public string Label { get; set; } = string.Empty; // e.g., "14:00" or "Mon 24"
    public decimal TotalEnergyKwh { get; set; }
    public double AverageActivePowerWatts { get; set; }
    public double MinActivePowerWatts { get; set; }
    public double MaxActivePowerWatts { get; set; }
    public double AverageVoltageVolts { get; set; }
    public double TotalCurrentAmps { get; set; }
    public double AveragePowerFactor { get; set; }
    public int DataPointsCount { get; set; }
    public decimal EstimatedCost { get; set; }
}