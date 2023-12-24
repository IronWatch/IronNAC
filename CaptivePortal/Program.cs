using Amazon.Route53;
using CaptivePortal;
using CaptivePortal.Components;
using CaptivePortal.Database;
using CaptivePortal.Database.Entities;
using CaptivePortal.Services;
using LettuceEncrypt;
using LettuceEncrypt.Acme;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

ILogger logger = LoggerFactory.Create(l => l
    .SetMinimumLevel(LogLevel.Trace)
    .AddConsole())
    .CreateLogger<Program>();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    string hostName = builder.Configuration.GetValue<string>("CaptivePortal:HostName")
        ?? throw new MissingFieldException("CaptivePortal:HostName");

    List<string> listenAddresses = builder.Configuration.GetSection("CaptivePortal:ListenAddresses")
        .Get<string[]>()
        ?.ToList()
        ?? throw new MissingFieldException("CaptivePortal:ListenAddresses");

    builder.WebHost.UseKestrel(kestrel =>
    {
        foreach (string address in listenAddresses)
        {
            kestrel.Listen(IPAddress.Parse(address), 80);
            kestrel.Listen(IPAddress.Parse(address), 443, listenOpts =>
            {
                listenOpts.UseHttps(https =>
                {
                    https.UseLettuceEncrypt(kestrel.ApplicationServices);
                });
            });
        }
    });

    builder.Services.AddLettuceEncrypt(async opts =>
    {
        if (opts.UseStagingServer)
        {
            logger.LogInformation("Using the Lets Encrypt staging environment. Checking for stage root certs!");

            List<string> stageRootCertUris = builder.Configuration.GetSection("LetsEncrypt:StageRootCerts")
                .Get<string[]>()
                ?.ToList()
                ?? throw new MissingFieldException("LetsEncrypt:StageRootCerts");
            List<string> stageRootCertContents = new();

            using HttpClient issuersHttpClient = new();

            foreach (string stageRootCertUri in stageRootCertUris)
            {
                logger.LogInformation("Downloading Lets Encrypt Staging Certificate: {uri}", stageRootCertUri);
                stageRootCertContents.Add(await issuersHttpClient.GetStringAsync(stageRootCertUri));
            }

            opts.AdditionalIssuers = stageRootCertContents.ToArray();
        }
    });
    builder.Services.Replace(new ServiceDescriptor(typeof(IDnsChallengeProvider), typeof(AppDnsChallengeProvider), ServiceLifetime.Singleton));

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddTransient<RadiusDisconnector>();

    builder.Services.AddSingleton<RadiusAttributeParserService>();

    builder.Services.AddHostedService<RadiusAuthorizationBackgroundService>();
    builder.Services.AddHostedService<RadiusAccountingBackgroundService>();
    builder.Services.AddHostedService<DnsRedirectionServerBackgroundService>();

    builder.Services.AddDbContext<CaptivePortalDbContext>();

    builder.Services.AddScoped<WebAuthenticationService>();

    WebApplication app = builder.Build();

    using (IServiceScope scope = app.Services.CreateScope())
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
                IsStaff = true,
                IsAdmin = true
            };
            db.Users.Add(firstUser);
            await db.SaveChangesAsync();

            logger.LogInformation("Created Initial Administrator with\nEmail: {email}\nPassword: {password}", firstUser.Email, initialPassword);
        }
    }

    List<string> hostWhitelist = builder.Configuration
        .GetSection("CaptivePortal:HostRedirectionBypass")
        .Get<string[]>()
        ?.ToList()
        ?? throw new MissingFieldException("CaptivePortal:HostRedirectionBypass");

    app.Use(async (context, next) =>
    {
        // If we're not on the host whitelist, redirect to the captive portal
        if (!hostWhitelist.Where(x => x.Equals(context.Request.Host.Host, StringComparison.InvariantCultureIgnoreCase)).Any())
        {
            context.Response.StatusCode = 302;
            context.Response.Headers["Location"] = $"http://{hostName}/portal?redirect={context.Request.GetEncodedUrl()}";
            return;
        }

        // If we're accessing via fqdn and not https redirect to https
        if (context.Request.Scheme == "http" && context.Request.Host.Host == hostName)
        {
            string redirectLocation = context.Request.GetEncodedUrl();
            redirectLocation = $"https{redirectLocation.AsSpan().Slice(4)}";
            context.Response.StatusCode = 302;
            context.Response.Headers["Location"] = redirectLocation;
            return;
        }

        await next(context);
    });

    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "A Critical Exception Occured!");
}
