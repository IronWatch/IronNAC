using CaptivePortal.Database.Entities;
using CaptivePortal.Services;
using Microsoft.EntityFrameworkCore;
using static System.Formats.Asn1.AsnWriter;

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
        private readonly ILogger logger;

        public CaptivePortalDbContext(
            IConfiguration configuration, 
            ILogger<CaptivePortalDbContext> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(configuration.GetConnectionString("Database"));
        }

        public async Task SeedDatabase(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Processing Database Seeding Data");

            string initialPassword = "password";
            User firstUser = new()
            {
                Name = "Default Administrator",
                Email = "admin@localhost",
                Hash = WebAuthenticationService.GetHash(initialPassword),
                ChangePasswordNextLogin = true,
                PermissionLevel = CaptivePortal.Models.PermissionLevel.Admin
            };
            this.Users.Add(firstUser);
            logger.LogInformation("Seeded Default Administrator {email} with {password}", firstUser.Email, initialPassword);

            NetworkGroup registrationNetworkGroup = new()
            {
                Registration = true,
                IsPool = true,
                Name = "Registration Networks"
            };
            this.NetworkGroups.Add(registrationNetworkGroup);
            logger.LogInformation("Seeded Registration Network Group");

            NetworkGroup guestNetworkGroup = new()
            {
                Guest = true,
                IsPool = true,
                Name = "Student / Guest Network Pool"
            };
            this.NetworkGroups.Add(guestNetworkGroup);
            logger.LogInformation("Seeded Guest Network Group");

            await this.SaveChangesAsync(cancellationToken);
        }
    }
}
