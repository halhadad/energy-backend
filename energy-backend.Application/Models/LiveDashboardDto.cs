namespace energy_backend.Application.Models;

public class LiveReadingDto
{
    public DateTime Timestamp { get; set; }
    public double ActivePowerWatts { get; set; }
    public double VoltageVolts { get; set; }
    public double CurrentAmps { get; set; }
    public double PowerFactor { get; set; }
    public decimal CostRatePerHour { get; set; }
}

public class LiveSnapshotDto
{
    public Guid OrgId { get; set; }
    public DateTime SnapshotAt { get; set; }
    public decimal CurrentRatePerKwh { get; set; }
    public List<LiveReadingDto> Readings { get; set; } = new();
}
