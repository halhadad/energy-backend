using energy_backend.Application.Models.SignalR;

namespace energy_backend.Application.Models
{
    public class OrganisationAnalyticsDto
    {
        // ── Pie charts (device type breakdown) ───────────────────────────────
        public BreakdownDto PieChartDay { get; set; } = new();
        public BreakdownDto PieChartWeek { get; set; } = new();
        public BreakdownDto PieChartMonth { get; set; } = new();

        // ── Consumption time series (kWh per bucket) ─────────────────────────
        public TimeSeriesDto ConsumptionChartDay { get; set; } = new();
        public TimeSeriesDto ConsumptionChartWeek { get; set; } = new();
        public TimeSeriesDto ConsumptionChartMonth { get; set; } = new();

        // ── Cost time series (currency per bucket) ───────────────────────────
        public TimeSeriesDto CostChartDay { get; set; } = new();
        public TimeSeriesDto CostChartWeek { get; set; } = new();
        public TimeSeriesDto CostChartMonth { get; set; } = new();

        // ── Carbon time series (kg CO₂ per bucket) ───────────────────────────
        public TimeSeriesDto CarbonChartDay { get; set; } = new();
        public TimeSeriesDto CarbonChartWeek { get; set; } = new();
        public TimeSeriesDto CarbonChartMonth { get; set; } = new();

        // ── Live snapshot ─────────────────────────────────────────────────────

        /// <summary>
        /// Sum of AveragePowerWatts across all devices for the latest minute
        /// bucket. This is what the "Current Consumption" card shows.
        /// </summary>
        public float CurrentPowerWatts { get; set; }

        /// <summary>
        /// Sum of RatedPowerWatts across all devices in this organisation.
        /// Used as the midpoint "rated" marker on the consumption progress bar.
        /// </summary>
        public float TotalRatedPowerWatts { get; set; }

        // ── Month-to-date totals ──────────────────────────────────────────────

        /// <summary>Total energy consumed this month, kWh.</summary>
        public float Consumption { get; set; }

        /// <summary>Estimated cost this month in the configured currency.</summary>
        public float Cost { get; set; }

        /// <summary>Estimated carbon emissions this month, kg CO₂.</summary>
        public float Carbon { get; set; }

        // ── Organisation settings ─────────────────────────────────────────────

        /// <summary>
        /// Power budget in Watts set by the user on the Organisation.
        /// Used as the upper bound of the consumption progress bar.
        /// </summary>
        public float EnergyBudget { get; set; }
    }
}