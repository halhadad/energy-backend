namespace energy_backend.Application.Models.SignalR;

public class RealTimeEnergyIntervalDto
{
    public Guid OrgId { get; set; }
    public string Range { get; set; } = string.Empty;
    public int IntervalSizeMinutes { get; set; }
    public long Sequence { get; set; }
    public long Version { get; set; }
    public List<EnergyIntervalSummaryDto> Intervals { get; set; } = new();
    public List<DeviceIntervalSeriesDto> DeviceSeries { get; set; } = new();
}

public class EnergyIntervalSummaryDto
{
    public DateTime Timestamp { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal TotalEnergy { get; set; }
    public double AverageWatts { get; set; }
    public double AverageVoltage { get; set; }
    public double TotalCurrent { get; set; }
    public decimal EstimatedCost { get; set; }
    public List<DeviceEnergyContributionDto> DeviceContributions { get; set; } = new();
}

public class DeviceIntervalSeriesDto
{
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public List<EnergyIntervalSummaryDto> Intervals { get; set; } = new();
}

public class DeviceEnergyContributionDto
{
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public decimal TotalEnergyKwh { get; set; }
}
