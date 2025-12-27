using Microsoft.EntityFrameworkCore;
using BarberApp.Domain.Entities;

namespace BarberApp.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets (Tables)
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Barber> Barbers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== ROLE CONFIGURATION ==========
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Description).HasMaxLength(200);
                entity.HasIndex(r => r.Name).IsUnique();
            });

            // ========== USER CONFIGURATION ==========
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Phone).IsRequired().HasMaxLength(20);
                entity.Property(u => u.PasswordHash).IsRequired();

                // Confidentiality: Sensitive data field
                entity.Property(u => u.SensitiveData).HasColumnType("text");

                // Indexes for performance
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Phone);

                // Relationship: User -> Role
                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship: User -> Barber (One-to-One optional)
                entity.HasOne(u => u.Barber)
                    .WithOne(b => b.User)
                    .HasForeignKey<Barber>(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== BARBER CONFIGURATION ==========
            modelBuilder.Entity<Barber>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Specialty).HasMaxLength(100);
                entity.Property(b => b.Rating).HasPrecision(3, 2); // Max 5.00
                entity.Property(b => b.Availability).HasColumnType("text");
            });

            // ========== SERVICE CONFIGURATION ==========
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Description).HasMaxLength(500);
                entity.Property(s => s.Price).HasPrecision(10, 2);
                entity.HasIndex(s => s.Name);
            });

            // ========== APPOINTMENT CONFIGURATION ==========
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Status).IsRequired().HasMaxLength(20);
                entity.Property(a => a.Notes).HasMaxLength(1000);

                // Relationship: Appointment -> Client (User)
                entity.HasOne(a => a.Client)
                    .WithMany(u => u.AppointmentsAsClient)
                    .HasForeignKey(a => a.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship: Appointment -> Barber
                entity.HasOne(a => a.Barber)
                    .WithMany(b => b.Appointments)
                    .HasForeignKey(a => a.BarberId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship: Appointment -> Service
                entity.HasOne(a => a.Service)
                    .WithMany(s => s.Appointments)
                    .HasForeignKey(a => a.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes for queries
                entity.HasIndex(a => a.DateTime);
                entity.HasIndex(a => a.Status);
                entity.HasIndex(a => new { a.BarberId, a.DateTime });
            });

            // ========== SEED DATA (Initial Roles) ==========
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Client", Description = "Regular client", IsActive = true },
                new Role { Id = 2, Name = "Barber", Description = "Barber professional", IsActive = true },
                new Role { Id = 3, Name = "Manager", Description = "Barber manager with extended permissions", IsActive = true },
                new Role { Id = 4, Name = "Administrator", Description = "System administrator", IsActive = true },
                new Role { Id = 5, Name = "Auditor", Description = "External auditor (temporary access)", IsActive = true }
            );
        }
    }
}