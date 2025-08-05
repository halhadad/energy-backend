using energy_backend.Core.Entities;
using energy_backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Data
{
    public class EnergyDbContext(DbContextOptions<EnergyDbContext> options) : DbContext(options)
    {
        public DbSet<Energy> Energies => Set<Energy>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Organisation> Organisations => Set<Organisation>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<Setting> Settings => Set<Setting>();
        public DbSet<Alert> Alerts => Set<Alert>();
        public DbSet<EnergyReading> EnergyReadings => Set<EnergyReading>();
        public DbSet<AggregatedEnergy> AggregatedEnergies => Set<AggregatedEnergy>();

    }
}
