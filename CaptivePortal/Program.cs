using CaptivePortal;
using CaptivePortal.Components;
using CaptivePortal.Database;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel()
    .UseUrls("http://0.0.0.0:80");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<RadiusDisconnector>();

builder.Services.AddSingleton<RadiusAttributeParserService>();

builder.Services.AddHostedService<RadiusAuthorizationBackgroundService>();
builder.Services.AddHostedService<RadiusAccountingBackgroundService>();
builder.Services.AddHostedService<DnsRedirectionServerBackgroundService>();
builder.Services.AddDbContext<CaptivePortalDbContext>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    CaptivePortalDbContext db = scope.ServiceProvider.GetRequiredService<CaptivePortalDbContext>();
    db.Database.Migrate();
}

List<string> hostWhitelist = [
    "10.100.0.100"
];

app.Use(async (context, next) =>
{
    if (!hostWhitelist.Where(x => x.Equals(context.Request.Host.Host, StringComparison.InvariantCultureIgnoreCase)).Any())
    {
        context.Response.StatusCode = 302;
        context.Response.Headers["Location"] = $"http://10.100.0.100/portal?redirect={context.Request.GetEncodedUrl()}";
        return;
    }

    await next(context);
});

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
