using energy_backend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace energy_backend.Infrastructure.Data
{
    public class EnergyDbContext(DbContextOptions<EnergyDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Organisation> Organisations => Set<Organisation>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<Setting> Settings => Set<Setting>();
        public DbSet<Alert> Alerts => Set<Alert>();
        public DbSet<EnergyReading> EnergyReadings => Set<EnergyReading>();
        public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
        public DbSet<AggregateMinuteEnergy> AggregateMinuteEnergies => Set<AggregateMinuteEnergy>();
        public DbSet<AggregateHourEnergy> AggregateHourEnergies => Set<AggregateHourEnergy>();
        public DbSet<AggregateDayEnergy> AggregateDayEnergies => Set<AggregateDayEnergy>();
        public DbSet<AggregateMonthEnergy> AggregateMonthEnergies => Set<AggregateMonthEnergy>();
        public DbSet<EnergyRate> EnergyRates => Set<EnergyRate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EnergyReading>()
                .HasIndex(e => new { e.DeviceId, e.Timestamp }).IsUnique();
            modelBuilder.Entity<EnergyReading>()
                .HasIndex(e => new { e.Timestamp, e.DeviceId });

            modelBuilder.Entity<AggregateMinuteEnergy>(entity =>
            {
                entity.HasIndex(a => new { a.DeviceId, a.Timestamp }).IsUnique();
                entity.HasIndex(a => new { a.OrgId, a.Timestamp });
                entity.HasOne(a => a.Device).WithMany().HasForeignKey(a => a.DeviceId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Organisation>().WithMany().HasForeignKey(a => a.OrgId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<AggregateHourEnergy>(entity =>
            {
                entity.HasIndex(a => new { a.DeviceId, a.Timestamp }).IsUnique();
                entity.HasIndex(a => new { a.OrgId, a.Timestamp });
                entity.HasOne(a => a.Device).WithMany().HasForeignKey(a => a.DeviceId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Organisation>().WithMany().HasForeignKey(a => a.OrgId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<AggregateDayEnergy>(entity =>
            {
                entity.HasIndex(a => new { a.DeviceId, a.Timestamp }).IsUnique();
                entity.HasIndex(a => new { a.OrgId, a.Timestamp });
                entity.HasOne(a => a.Device).WithMany().HasForeignKey(a => a.DeviceId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Organisation>().WithMany().HasForeignKey(a => a.OrgId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<AggregateMonthEnergy>(entity =>
            {
                entity.HasIndex(a => new { a.DeviceId, a.Timestamp }).IsUnique();
                entity.HasIndex(a => new { a.OrgId, a.Timestamp });
                entity.HasOne(a => a.Device).WithMany().HasForeignKey(a => a.DeviceId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Organisation>().WithMany().HasForeignKey(a => a.OrgId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Organisation>(entity =>
            {
                entity.Property(o => o.MonthlyBudgetUsd).HasColumnType("decimal(18,4)");
            });

            // Effective-dated rate timeline per organisation.
            modelBuilder.Entity<EnergyRate>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasIndex(r => new { r.OrganisationId, r.ValidFromUtc })
                      .IsUnique()
                      .HasDatabaseName("IX_EnergyRates_OrgId_ValidFrom");
                entity.Property(r => r.RatePerKwh).HasColumnType("decimal(18,6)");
                entity.HasOne<Organisation>()
                      .WithMany()
                      .HasForeignKey(r => r.OrganisationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}