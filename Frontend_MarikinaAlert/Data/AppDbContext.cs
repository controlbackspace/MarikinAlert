using Frontend_MarikinaAlert.Models;
using Microsoft.EntityFrameworkCore;

namespace Frontend_MarikinaAlert.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<DisasterReport> DisasterReports => Set<DisasterReport>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var report = modelBuilder.Entity<DisasterReport>();

            // Store enums as text so the database stays readable in DB Browser for SQLite.
            report.Property(r => r.Category).HasConversion<string>().HasMaxLength(20);
            report.Property(r => r.Priority).HasConversion<string>().HasMaxLength(20);
            report.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            // Separate int column for sorting, since text sorts "Critical" < "High" < "Normal" alphabetically.
            report.Property<int>("PriorityRank");

            report.HasIndex(r => r.Status);
            report.HasIndex(r => r.CreatedAtUtc);
        }

        public override int SaveChanges()
        {
            SyncPriorityRank();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SyncPriorityRank();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void SyncPriorityRank()
        {
            foreach (var entry in ChangeTracker.Entries<DisasterReport>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                    entry.Property("PriorityRank").CurrentValue = (int)entry.Entity.Priority;
            }
        }
    }
}
