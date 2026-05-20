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

        // ── Cost time series (currency per bucket) ────────────────────────────
        public TimeSeriesDto CostChartDay { get; set; } = new();
        public TimeSeriesDto CostChartWeek { get; set; } = new();
        public TimeSeriesDto CostChartMonth { get; set; } = new();

        // ── Live snapshot ──────────────────────────────────────────────────────

        /// <summary>Sum of AverageActivePowerWatts across all devices for the latest minute bucket.</summary>
        public float CurrentPowerWatts { get; set; }

        /// <summary>Average line voltage across all devices for the latest minute bucket.</summary>
        public float AverageVoltageVolts { get; set; }

        /// <summary>Total current across all devices for the latest minute bucket, in Amperes.</summary>
        public float TotalCurrentAmps { get; set; }

        /// <summary>Power-weighted average power factor across all devices for the latest minute.</summary>
        public float AveragePowerFactor { get; set; }

        /// <summary>Sum of RatedPowerWatts across all devices in this organisation.</summary>
        public float TotalRatedPowerWatts { get; set; }

        // ── Month-to-date totals ───────────────────────────────────────────────

        /// <summary>Total energy consumed this month, kWh.</summary>
        public float Consumption { get; set; }

        /// <summary>Estimated cost this month in the user's configured currency.</summary>
        public float Cost { get; set; }

        // ── Organisation settings ──────────────────────────────────────────────

        /// <summary>Power budget in Watts set by the user on the Organisation.</summary>
        public float EnergyBudget { get; set; }

        /// <summary>User's configured electricity rate (cost per kWh).</summary>
        public float ElectricityCostPerKwh { get; set; }
    }
}