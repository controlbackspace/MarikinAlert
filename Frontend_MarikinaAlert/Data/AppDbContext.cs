using Frontend_MarikinaAlert.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Frontend_MarikinaAlert.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // These two lines tell the database: "Please create tables for these two classes."
        public DbSet<DisasterReport> DisasterReports { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // LOGIC: Fine-tuning the database rules
            modelBuilder.Entity<DisasterReport>(entity =>
            {
                // Ensure the 'Id' is treated as the primary key
                entity.HasKey(e => e.Id);

                // Requirement: RawMessage cannot be empty
                entity.Property(e => e.RawMessage).IsRequired();

                // Requirement: High precision for GPS (standard is 10 digits, 7 decimals)
                entity.Property(e => e.Latitude).HasPrecision(10, 7);
                entity.Property(e => e.Longitude).HasPrecision(10, 7);
            });

            // LOGIC: We "Seed" (pre-fill) a default admin user so you can log in immediately.
            modelBuilder.Entity<AdminUser>().HasData(
                new AdminUser
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "admin123", // In a real app, we would encrypt this!
                    AssignedNodeName = "MasterNode"
                }
            );
        }
    }
}