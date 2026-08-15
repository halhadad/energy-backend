using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Core.Entities;

namespace energy_backend.Core.Interfaces;

public interface IAlertRepository
{
    Task<IEnumerable<Alert>> GetByUserIdAsync(Guid userId);
    Task<Alert?> GetByIdAsync(Guid userId, Guid alertId);

    Task<List<Alert>> GetAllWithOrganisationsAsync();

    Task<IEnumerable<AlertEvent>> GetRecentEventsByUserIdAsync(Guid userId, int count = 50);
    Task AddAsync(Alert alert);
    Task AddEventAsync(AlertEvent alertEvent);
    Task DeleteAsync(Alert alert);
    Task SaveChangesAsync();
}
