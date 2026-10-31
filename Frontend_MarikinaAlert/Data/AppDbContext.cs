using Frontend_MarikinaAlert.Models;
using Microsoft.EntityFrameworkCore;

namespace Frontend_MarikinaAlert.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<DisasterReport> DisasterReports => Set<DisasterReport>();
        public DbSet<Agency> Agencies => Set<Agency>();
        public DbSet<ContactAttempt> ContactAttempts => Set<ContactAttempt>();
        public DbSet<ResponseTeam> ResponseTeams => Set<ResponseTeam>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var report = modelBuilder.Entity<DisasterReport>();

            // Store enums as text so the database stays readable in DB Browser for SQLite.
            report.Property(r => r.Category).HasConversion<string>().HasMaxLength(20);
            report.Property(r => r.Priority).HasConversion<string>().HasMaxLength(20);
            report.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            report.Property(r => r.Source).HasConversion<string>().HasMaxLength(20);

            // Separate int column for sorting, since text sorts "Critical" < "High" < "Normal" alphabetically.
            report.Property<int>("PriorityRank");

            report.HasIndex(r => r.Status);
            report.HasIndex(r => r.CreatedAtUtc);
            report.HasIndex(r => r.TrackingCode);

            modelBuilder.Entity<Agency>().HasIndex(a => a.Name).IsUnique();

            var contact = modelBuilder.Entity<ContactAttempt>();
            contact.Property(c => c.Method).HasConversion<string>().HasMaxLength(20);
            contact.Property(c => c.Outcome).HasConversion<string>().HasMaxLength(20);
            contact.HasOne<DisasterReport>().WithMany().HasForeignKey(c => c.ReportId).OnDelete(DeleteBehavior.Cascade);
            contact.HasIndex(c => c.ReportId);

            var team = modelBuilder.Entity<ResponseTeam>();
            team.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            team.HasIndex(t => t.Name).IsUnique();
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
