using CaptivePortal.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaptivePortal.Database
{
    public class CaptivePortalDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Network> Networks { get; set; }
        public DbSet<DeviceNetwork> DeviceNetworks { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<NetworkGroup> NetworkGroups { get; set; }
        public DbSet<UserNetworkGroup> UserNetworkGroups { get; set; }

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
