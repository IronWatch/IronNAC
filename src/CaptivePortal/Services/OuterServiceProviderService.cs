using CaptivePortal.Daemons;
using CaptivePortal.Database;
using Microsoft.EntityFrameworkCore;

namespace CaptivePortal.Services
{
    public class OuterServiceProviderService
    {
        public IServiceProvider OuterServiceProvider { get; private set; }

        public OuterServiceProviderService(IServiceProvider outerSP)
        {
            this.OuterServiceProvider = outerSP;
        }

        public AppArgsService AppArgsService
            => OuterServiceProvider.GetRequiredService<AppArgsService>();
        
        public RadiusDisconnectorService RadiusDisconnectorService
            => OuterServiceProvider.GetRequiredService<RadiusDisconnectorService>();

        public RadiusAttributeParserService RadiusAttributeParserService
            => OuterServiceProvider.GetRequiredService<RadiusAttributeParserService>();

        public WebAuthenticationService WebAuthenticationService
            => OuterServiceProvider.GetRequiredService<WebAuthenticationService>();

        public IDbContextFactory<CaptivePortalDbContext> DbContextFactory
            => OuterServiceProvider.GetRequiredService<IDbContextFactory<CaptivePortalDbContext>>();

        public WebDaemon WebDaemon
            => OuterServiceProvider.GetRequiredService<WebDaemon>();

        public DnsDaemon DnsDaemon
            => OuterServiceProvider.GetRequiredService<DnsDaemon>();

        public RadiusAuthorizationDaemon RadiusAuthorizationDaemon
            => OuterServiceProvider.GetRequiredService<RadiusAuthorizationDaemon>();

        public RadiusAccountingDaemon RadiusAccountingDaemon
            => OuterServiceProvider.GetRequiredService<RadiusAccountingDaemon>();

        public static void RegisterServicesInParent(IServiceCollection services, string[] appArgs)
        {
            services.AddSingleton(new AppArgsService(appArgs));

            services.AddTransient<RadiusDisconnectorService>();

            services.AddSingleton<RadiusAttributeParserService>();

            services.AddScoped<WebAuthenticationService>();

            services.AddDbContextFactory<CaptivePortalDbContext>();

            services.AddHostedService<WebDaemon>();

            services.AddHostedService<DnsDaemon>();

            services.AddHostedService<RadiusAuthorizationDaemon>();

            services.AddHostedService<RadiusAccountingDaemon>();
        }

        public void RegisterServicesInChild(IServiceCollection services)
        {
            services.AddSingleton(this);
            
            services.AddSingleton(AppArgsService);

            services.AddTransient(sp 
                => sp.GetRequiredService<OuterServiceProviderService>().RadiusDisconnectorService);

            services.AddSingleton(RadiusAttributeParserService);

            services.AddScoped(sp
                => sp.GetRequiredService<OuterServiceProviderService>().WebAuthenticationService);

            services.AddSingleton(sp
                => sp.GetRequiredService<OuterServiceProviderService>().DbContextFactory);

            // Accessing hosted services must be through the Outer Service Provider Service itself
            // otherwise a nesting loop of hosted services will be launched
            services.AddSingleton<DaemonInteractionService>();
        }
    }
}
