using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace energy_backend.Application.Models
{
    public class AlertResponseDto
    {
        public Guid AlertId { get; set; }
        public string Name { get; set; } = string.Empty;
        public float Threshold { get; set; }
        public float EnergyConsumption { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
    }
}
