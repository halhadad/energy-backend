using System.Collections.Generic;

namespace energy_backend.Application.Models
{
    public class AggregationResultDto
    {
        public List<TimeSeriesDataPoint> DataPoints { get; set; } = new List<TimeSeriesDataPoint>();
        public string AggregationPeriod { get; set; } = string.Empty; // e.g., "Hourly", "Daily"
        public double TotalValue { get; set; } // Total aggregated value over the period
        // Add other relevant summary statistics if needed
    }
}
