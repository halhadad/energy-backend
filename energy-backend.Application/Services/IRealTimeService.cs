using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Application.Models.SignalR;

namespace energy_backend.Application.Services
{
    public interface IRealTimeService
    {
        Task<RealTimeDto> GetOverviewDataAsync(Guid organisationId);
    }
}
