namespace energy_backend.Application.Models;

public class OrganisationAnalyticsDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public AnalyticsSummaryDto Summary { get; set; } = new();
    public List<DeviceBreakdownDto> DeviceBreakdown { get; set; } = new();

    public List<EnergyMetricRollupDto> MinuteRollups { get; set; } = new();
    public List<EnergyMetricRollupDto> HourRollups { get; set; } = new();
    public List<EnergyMetricRollupDto> DayRollups { get; set; } = new();
    public List<EnergyMetricRollupDto> MonthRollups { get; set; } = new();
}
