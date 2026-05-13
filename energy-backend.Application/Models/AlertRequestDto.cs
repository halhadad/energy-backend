using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace energy_backend.Application.Models
{
    public class AlertRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid OrganisationId { get; set; }
        public float Threshold { get; set; }
    }
}
