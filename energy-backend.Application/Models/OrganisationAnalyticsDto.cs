using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        // so-far metrics for the month
        public float Consumption { get; set; }
        public float Cost { get; set; }
        public float Carbon { get; set; }
    }
}
