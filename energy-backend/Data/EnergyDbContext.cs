using energy_backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Data
{
    public class EnergyDbContext(DbContextOptions<EnergyDbContext> options) : DbContext(options)
    {
        public DbSet<Energy> Energies => Set<Energy>();
        public DbSet<User> Users => Set<User>();

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //    modelBuilder.Entity<Energy>().HasData(new Energy
        //    {
        //        id = 1,
        //        CurrentConsumption = 150.5f,
        //        TotalConsumption = 1200.75f
        //    });

        //}
    }
}
