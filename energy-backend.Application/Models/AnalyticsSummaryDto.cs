namespace energy_backend.Application.Models;

public class AnalyticsSummaryDto
{
    public double TotalEnergyKwh { get; set; }
    public decimal TotalCost { get; set; }

    public double PeakPowerWatts { get; set; }
    public DateTime? PeakAt { get; set; }
    public double MinPowerWatts { get; set; }

    // budget is tracked against the current calendar month
    public double MonthToDateKwh { get; set; }
    public decimal MonthToDateCostUsd { get; set; }
    public decimal MonthlyBudgetUsd { get; set; }
    public double PercentOfBudget { get; set; }
    public double ProjectedMonthKwh { get; set; }
    public decimal ProjectedMonthCostUsd { get; set; }

    public PeriodComparisonDto? PreviousPeriod { get; set; }
}

public class PeriodComparisonDto
{
    public double TotalEnergyKwh { get; set; }
    public decimal TotalCost { get; set; }
    public double EnergyChangePercent { get; set; }
}

public class DeviceBreakdownDto
{
    public string DeviceName { get; set; } = string.Empty;
    public double TotalEnergyKwh { get; set; }
    public double SharePercent { get; set; }
}
