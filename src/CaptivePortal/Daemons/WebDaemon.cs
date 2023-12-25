using CaptivePortal.Components;
using CaptivePortal.Database.Entities;
using CaptivePortal.Database;
using CaptivePortal.Services;
using LettuceEncrypt.Acme;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CaptivePortal.Daemons
{
    public class WebDaemon(
        IConfiguration configuration,
        ILogger<WebDaemon> logger,
        AppArgsService appArgsService,
        IServiceProvider outerServiceProvider) 
        : BaseDaemon<WebDaemon>(configuration, logger)
    {
        protected override async Task EntryPoint(CancellationToken cancellationToken)
        {
            try
            {
                WebApplicationBuilder builder = WebApplication.CreateBuilder(appArgsService.Args);

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

                OuterServiceProviderService outerSpService = new(outerServiceProvider);
                outerSpService.RegisterServicesInChild(builder.Services);

                builder.Services.AddLettuceEncrypt(async opts =>
                {
                    if (opts.UseStagingServer)
                    {
                        Logger.LogInformation("Using the Lets Encrypt staging environment. Checking for stage root certs!");

                        List<string> stageRootCertUris = builder.Configuration.GetSection("LetsEncrypt:StageRootCerts")
                            .Get<string[]>()
                            ?.ToList()
                            ?? throw new MissingFieldException("LetsEncrypt:StageRootCerts");
                        List<string> stageRootCertContents = new();

                        using HttpClient issuersHttpClient = new();

                        foreach (string stageRootCertUri in stageRootCertUris)
                        {
                            Logger.LogInformation("Downloading Lets Encrypt Staging Certificate: {uri}", stageRootCertUri);
                            stageRootCertContents.Add(await issuersHttpClient.GetStringAsync(stageRootCertUri));
                        }

                        opts.AdditionalIssuers = stageRootCertContents.ToArray();
                    }
                });
                builder.Services.Replace(
                    new ServiceDescriptor(
                        typeof(IDnsChallengeProvider), 
                        typeof(PublicDnsChallengeProvider), 
                        ServiceLifetime.Singleton));

                builder.Services.AddRazorComponents()
                    .AddInteractiveServerComponents();

                builder.Services.AddHttpContextAccessor();

                builder.Services.AddDbContext<CaptivePortalDbContext>();

                WebApplication app = builder.Build();

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
                        context.Response.Headers["Location"] 
                            = $"http://{hostName}/portal?redirect={context.Request.GetEncodedUrl()}";
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

                app.UseStatusCodePagesWithRedirects("/{0}");

                Logger.LogInformation("{listener} started", nameof(WebDaemon));
                this.Running = true;

                await app.RunAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "The WebDaemon has crashed with a critical exception!");
            }
        }
    }
}
