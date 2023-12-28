using CaptivePortal.Services;
using CaptivePortal.Services.Outer;
using Microsoft.EntityFrameworkCore.Design;

namespace CaptivePortal.Database
{
    public class IronNacDbContextDesignTimeFactory
        : IDesignTimeDbContextFactory<IronNacDbContext>
    {
        public IronNacDbContext CreateDbContext(string[] args)
        {
            ILogger<IronNacDbContext> logger = LoggerFactory.Create(l => l
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole())
                .CreateLogger<IronNacDbContext>();

            return new IronNacDbContext(new IronNacConfiguration(), logger);
        }
    }
}
