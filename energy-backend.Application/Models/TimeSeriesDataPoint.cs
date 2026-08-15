using System;

namespace energy_backend.Application.Models
{
    public class TimeSeriesDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string? Label { get; set; }
        public double AverageWatts { get; set; }
        public double AverageVoltageVolts { get; set; }
        public double TotalCurrentAmps { get; set; }
        public double AveragePowerFactor { get; set; }
        public Dictionary<string, double> DeviceBreakdown { get; set; } = new();
    }
}
