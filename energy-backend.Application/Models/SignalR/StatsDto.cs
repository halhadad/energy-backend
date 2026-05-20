namespace energy_backend.Application.Models.SignalR
{
    /// <summary>
    /// Live snapshot stats for the overview dashboard cards.
    /// Pushed via SignalR on each minute aggregate update.
    /// Carbon has been removed; electrical quality metrics added.
    /// </summary>
    public class StatsDto
    {
        // ── Power ─────────────────────────────────────────────────────────────
        /// <summary>Sum of AverageActivePowerWatts across all devices (latest minute bucket).</summary>
        public float CurrentActivePowerWatts { get; set; }

        // ── Electrical quality ────────────────────────────────────────────────
        /// <summary>Average line voltage across all devices (latest minute bucket), Volts.</summary>
        public float AverageVoltageVolts { get; set; }

        /// <summary>Total current drawn by all devices (latest minute bucket), Amperes.</summary>
        public float TotalCurrentAmps { get; set; }

        /// <summary>Power-weighted average power factor across all devices (latest minute).</summary>
        public float AveragePowerFactor { get; set; }

        // ── Cost ──────────────────────────────────────────────────────────────
        /// <summary>Estimated cost today (midnight to now), in user's configured currency.</summary>
        public float TodaysCost { get; set; }

        /// <summary>Estimated cost this month (1st to now), in user's configured currency.</summary>
        public float MonthlyCost { get; set; }

        // ── Budget ────────────────────────────────────────────────────────────
        public float PowerBudgetWatts { get; set; }
    }
}