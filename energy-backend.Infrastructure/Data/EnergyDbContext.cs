using energy_backend.Core.Entities;
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
        public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
        public DbSet<DeviceConsumptionSummary> DeviceConsumptionSummaries => Set<DeviceConsumptionSummary>();

        // New Aggregate tables (per-device)
        public DbSet<AggregateMinuteEnergy> AggregateMinuteEnergies => Set<AggregateMinuteEnergy>();
        public DbSet<AggregateHourEnergy> AggregateHourEnergies => Set<AggregateHourEnergy>();
        public DbSet<AggregateDayEnergy> AggregateDayEnergies => Set<AggregateDayEnergy>();
        public DbSet<AggregateMonthEnergy> AggregateMonthEnergies => Set<AggregateMonthEnergy>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EnergyReading>()
                .HasIndex(e => new { e.DeviceId, e.Timestamp })
                .IsUnique();

            modelBuilder.Entity<AggregatedEnergy>()
                .HasIndex(a => new { a.DeviceId, a.PeriodStartTime })
                .IsUnique();

            // Indexes for new Per-Device Aggregate tables
            // Indexes for new Per-Device Aggregate tables
            // --- AggregateMinuteEnergy ---
            modelBuilder.Entity<AggregateMinuteEnergy>(entity =>
            {
                entity.HasIndex(a => new { a.DeviceId, a.Timestamp }).IsUnique();

                entity.HasOne(a => a.Device)
                    .WithMany()
                    .HasForeignKey(a => a.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Organisation>() // Explicitly link to Organisation
                    .WithMany()
                    .HasForeignKey(a => a.OrgId)
                    .OnDelete(DeleteBehavior.NoAction); // Break the cycle
            });

            // --- AggregateHourEnergy ---
            modelBuilder.Entity<AggregateHourEnergy>(entity =>
            {
                entity.HasIndex(a => new { a.DeviceId, a.Timestamp }).IsUnique();

                entity.HasOne(a => a.Device)
                    .WithMany()
                    .HasForeignKey(a => a.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Organisation>()
                    .WithMany()
                    .HasForeignKey(a => a.OrgId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // --- AggregateDayEnergy ---
            modelBuilder.Entity<AggregateDayEnergy>(entity =>
            {
                entity.HasIndex(a => new { a.DeviceId, a.Timestamp }).IsUnique();

                entity.HasOne(a => a.Device)
                    .WithMany()
                    .HasForeignKey(a => a.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Organisation>()
                    .WithMany()
                    .HasForeignKey(a => a.OrgId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // --- AggregateMonthEnergy ---
            modelBuilder.Entity<AggregateMonthEnergy>(entity =>
            {
                entity.HasIndex(a => new { a.DeviceId, a.Timestamp }).IsUnique();

                entity.HasOne(a => a.Device)
                    .WithMany()
                    .HasForeignKey(a => a.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Organisation>()
                    .WithMany()
                    .HasForeignKey(a => a.OrgId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }

    
    
}
