namespace energy_backend.Application.Models;

public class DeviceMetricSummaryDto
{
    public Guid DeviceId { get; set; }
    public List<DevicePeriodMetricDto> Intervals { get; set; } = [];
}

public class DevicePeriodMetricDto
{
    public DateTime Timestamp { get; set; }
    public decimal TotalEnergyKwh { get; set; }
}