using Amazon.Route53;
using CaptivePortal.Components;
using CaptivePortal.Database;
using CaptivePortal.Database.Entities;
using CaptivePortal.Daemons;
using LettuceEncrypt.Acme;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using CaptivePortal.Services.Outer;
using System.CommandLine;

// Hotpath shortcut as we use the design time factory to build the dbcontext for a migration
// ef will still try and start up the application as part of detecting things, needlessly calling most of our startup code
if (EF.IsDesignTime)
{
    _ = WebApplication.CreateBuilder(args).Build();
    return;
}

Option<string> envOption = new("--env", "DotEnv formatted env file to load");
RootCommand rootCommand = [envOption];

rootCommand.SetHandler(async (envOptionValue) =>
{
    // Logger for this outer host startup
    // Basically just for database seeding purposes
    ILogger logger = LoggerFactory.Create(l => l
        .SetMinimumLevel(LogLevel.Trace)
        .AddConsole())
        .CreateLogger<Program>();

    if (envOptionValue is not null)
    {
        logger.LogInformation("Loading Environment Variables from \"{path}\"", envOptionValue);
        try
        {
            _ = await IronNacConfiguration.LoadDotEnvAsync(envOptionValue);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to load Environment Variables!");
            return;
        }
    }

    HostBuilder builder = new();

    builder.ConfigureLogging(loggingBuilder =>
    {
        loggingBuilder
            .SetMinimumLevel(LogLevel.Information)
            .AddConsole();
    });

    builder.ConfigureAppConfiguration(configurationBuilder =>
    {
        configurationBuilder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false);
    });

    bool servicesSuccess = false;
    builder.ConfigureServices(services =>
    {
        /* The outer service provider registers and handles service injection
         * Within it, the IHostedServices are registered, as well as any outer services the IHostedServices need
         *
         * For nested application hosts, the OuterServiceProviderService is used to resolve these outer services
         * from within the child's service provider.
        */

        servicesSuccess = OuterServiceProviderService.RegisterServicesInParent(services, logger, args);

        services.AddDbContext<IronNacDbContext>();
    });

    IHost host = builder.Build();

    if (!servicesSuccess) return;

    // Database is created / updated here and optionally seeded.
    // TODO move seed data to its own class
    using (IServiceScope scope = host.Services.CreateScope())
    {
        IronNacDbContext db = scope.ServiceProvider.GetRequiredService<IronNacDbContext>();

        bool creatingDb = db.Database.GetAppliedMigrations().Count() == 0;

        logger.LogInformation("Processing database migrations");
        db.Database.Migrate();

        if (creatingDb)
        {
            await db.SeedDatabase();
        }
    }

    await host.RunAsync();
}, envOption);

await rootCommand.InvokeAsync(args);
