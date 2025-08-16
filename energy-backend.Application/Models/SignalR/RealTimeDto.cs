using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace energy_backend.Application.Models.SignalR
{
    public class RealTimeDto
    {

        public BreakdownDto PieChartDay { get; set; } = new();
        public BreakdownDto PieChartWeek { get; set; } = new();
        public TimeSeriesDto LineChartDay { get; set; } = new();
        public TimeSeriesDto LineChartWeek { get; set; } = new();
        public StatsDto Stats { get; set; } = new();


    }
}
