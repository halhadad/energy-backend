namespace energy_backend.Application.Models.SignalR;

public class LiveTickDto
{
    public Guid OrgId { get; set; }
    public DateTime Timestamp { get; set; }
    public double ActivePowerWatts { get; set; }
    public double VoltageVolts { get; set; }
    public double CurrentAmps { get; set; }
    public double PowerFactor { get; set; }
    public decimal CostRatePerHour { get; set; }
}
