using CaptivePortal.Services;
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

            ILogger<CaptivePortalDbContext> logger = LoggerFactory.Create(l => l
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole())
                .CreateLogger<CaptivePortalDbContext>();

            return new CaptivePortalDbContext(configuration, logger);
        }
    }
}
