using System;

namespace energy_backend.Application.Models
{
    public class TimeSeriesDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string? Label { get; set; } // Optional label for display (e.g., "Mon", "10:00")
    }
}
