using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace energy_backend.Application.Models.SignalR
{
    public class TimeSeriesDto
    {
        public List<string> Labels { get; set; } = new();
        public List<float> Data { get; set; } = new();
    }
}
