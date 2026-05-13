using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Entities;

namespace energy_backend.Core.Interfaces
{
    public interface IAlertRepository
    {
    Task<IEnumerable<Alert>> GetByUserIdAsync(Guid userId);
    Task<Alert?> GetByIdAsync(Guid userId, Guid alertId);
    Task AddAsync(Alert alert);
    Task DeleteAsync(Alert alert);
    Task SaveChangesAsync();
}
}
