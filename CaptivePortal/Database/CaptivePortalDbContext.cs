using CaptivePortal.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaptivePortal.Database
{
    public class CaptivePortalDbContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }
        public DbSet<Device> Devices { get; set; }

        private readonly IConfiguration configuration;
        public CaptivePortalDbContext(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(configuration.GetConnectionString("Database"));
        }
    }
}
