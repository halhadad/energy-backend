using energy_backend.Application.Models.SignalR;

namespace energy_backend.Application.Models
{
    public class OrganisationAnalyticsDto
    {
        public BreakdownDto PieChartDay { get; set; } = new();
        public BreakdownDto PieChartWeek { get; set; } = new();
        public BreakdownDto PieChartMonth { get; set; } = new();
        public TimeSeriesDto ConsumptionChartDay { get; set; } = new();
        public TimeSeriesDto ConsumptionChartWeek { get; set; } = new();
        public TimeSeriesDto ConsumptionChartMonth { get; set; } = new();
        public TimeSeriesDto CostChartDay { get; set; } = new();
        public TimeSeriesDto CostChartWeek { get; set; } = new();
        public TimeSeriesDto CostChartMonth { get; set; } = new();
        public TimeSeriesDto CarbonChartDay { get; set; } = new();
        public TimeSeriesDto CarbonChartWeek { get; set; } = new();
        public TimeSeriesDto CarbonChartMonth { get; set; } = new();

        // Live current power in Watts (from latest minute aggregate summed across all devices)
        public float CurrentWatts { get; set; }

        // Month-to-date totals
        public float Consumption { get; set; }   // kWh this month
        public float Cost { get; set; }           // $ this month
        public float Carbon { get; set; }         // kg CO₂ this month

        // From Organisation.EnergyBudget
        public float EnergyBudget { get; set; }
    }
}