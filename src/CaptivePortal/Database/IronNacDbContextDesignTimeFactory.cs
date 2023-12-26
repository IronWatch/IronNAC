using CaptivePortal.Services;
using Microsoft.EntityFrameworkCore.Design;

namespace CaptivePortal.Database
{
    public class IronNacDbContextDesignTimeFactory
        : IDesignTimeDbContextFactory<IronNacDbContext>
    {
        public IronNacDbContext CreateDbContext(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            ILogger<IronNacDbContext> logger = LoggerFactory.Create(l => l
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole())
                .CreateLogger<IronNacDbContext>();

            return new IronNacDbContext(configuration, logger);
        }
    }
}
