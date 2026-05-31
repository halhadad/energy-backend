namespace energy_backend.Application.Models;

public class OrganisationAnalyticsDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Instead of calling them "Buckets", we call them what they are: Rollups or Metrics
    public List<EnergyMetricRollupDto> MinuteRollups { get; set; } = new();
    public List<EnergyMetricRollupDto> HourRollups { get; set; } = new();
    public List<EnergyMetricRollupDto> DayRollups { get; set; } = new();
    public List<EnergyMetricRollupDto> MonthRollups { get; set; } = new();
}