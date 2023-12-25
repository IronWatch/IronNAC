using Amazon.Route53;
using CaptivePortal.Components;
using CaptivePortal.Database;
using CaptivePortal.Database.Entities;
using CaptivePortal.Daemons;
using CaptivePortal.Services;
using LettuceEncrypt.Acme;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

// Hotpath shortcut as we use the design time factory to build the dbcontext for a migration
// ef will still try and start up the application as part of detecting things, needlessly calling most of our startup code
if (EF.IsDesignTime)
{
    _ = WebApplication.CreateBuilder(args).Build();
    return;
}

// Logger for this outer host startup
// Basically just for database seeding purposes
ILogger logger = LoggerFactory.Create(l => l
    .SetMinimumLevel(LogLevel.Trace)
    .AddConsole())
    .CreateLogger<Program>();

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

builder.ConfigureServices(services =>
{
    /* The outer service provider registers and handles service injection
     * Within it, the IHostedServices are registered, as well as any outer services the IHostedServices need
     *
     * For nested application hosts, the OuterServiceProviderService is used to resolve these outer services
     * from within the child's service provider.
    */
    OuterServiceProviderService.RegisterServicesInParent(services, args);
    services.AddDbContext<CaptivePortalDbContext>();
});

IHost host = builder.Build();

// Database is created / updated here and optionally seeded.
// TODO move seed data to its own class
using (IServiceScope scope = host.Services.CreateScope())
{
    CaptivePortalDbContext db = scope.ServiceProvider.GetRequiredService<CaptivePortalDbContext>();

    bool creatingDb = !db.Database.CanConnect();

    logger.LogInformation("Processing database migrations");
    db.Database.Migrate();

    if (creatingDb)
    {
        logger.LogInformation("Database was just created for the first time. Processing Seed Data");

        WebAuthenticationService webAuthService = scope.ServiceProvider.GetRequiredService<WebAuthenticationService>();

        string initialPassword = "password";
        User? firstUser = new()
        {
            Name = "Default Administrator",
            Email = "admin@localhost",
            Hash = webAuthService.GetHash(initialPassword),
            ChangePasswordNextLogin = true,
            PermissionLevel = CaptivePortal.Models.PermissionLevel.Admin
        };
        db.Users.Add(firstUser);
        await db.SaveChangesAsync();

        logger.LogInformation("Created Initial Administrator with\nEmail: {email}\nPassword: {password}", firstUser.Email, initialPassword);
    }
}

await host.RunAsync();
