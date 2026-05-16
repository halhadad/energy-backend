using System;
using System.Collections.Generic;

namespace energy_backend.Application.Models.SignalR
{
    public class RealTimeChartDataDto
    {
        public string Range { get; set; } = string.Empty;
        public List<RealTimeChartBucketDto> Buckets { get; set; } = new List<RealTimeChartBucketDto>();
    }

    public class RealTimeChartBucketDto
    {
        public DateTime Timestamp { get; set; }
        public float TotalEnergy { get; set; }
        public float AverageWatts { get; set; }
        public float MinWatts { get; set; }
        public float MaxWatts { get; set; }
        public int DataPointsCount { get; set; }
    }
}
