using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Core.Entities;

namespace energy_backend.Application.Services
{
    public interface IAlertsMonitorService
    {
        Task CheckAlertsAsync(CancellationToken ct);
    }
}
