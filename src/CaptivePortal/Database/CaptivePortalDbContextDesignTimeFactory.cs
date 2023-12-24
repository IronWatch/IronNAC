using Microsoft.EntityFrameworkCore.Design;

namespace CaptivePortal.Database
{
    public class CaptivePortalDbContextDesignTimeFactory
        : IDesignTimeDbContextFactory<CaptivePortalDbContext>
    {
        public CaptivePortalDbContext CreateDbContext(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            return new CaptivePortalDbContext(configuration);
        }
    }
}
