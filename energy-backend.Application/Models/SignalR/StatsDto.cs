using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace energy_backend.Application.Models.SignalR
{
    public class StatsDto
    {
        public float CurrentConsumption { get; set; }
        public float TodaysCost { get; set; }
        public float MonthlyBudget { get; set; }
        public float CarbonFootprint { get; set; }
    }
}
