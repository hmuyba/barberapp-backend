using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BarberApp.Infrastructure.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Connection string for migrations (design time)
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=barberapp_db;Username=postgres;Password=admin123");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}