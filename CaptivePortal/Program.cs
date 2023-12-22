using CaptivePortal;
using CaptivePortal.Components;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel()
    .UseUrls("http://0.0.0.0:80");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

/*builder.Services.AddHttpClient("IgnoreSSLErrors")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
    {
        ClientCertificateOptions = ClientCertificateOption.Manual,
        ServerCertificateCustomValidationCallback =
        (httpRequestMessage, cert, cetChain, policyErrors) =>
        {
            return true;
        }
    });

builder.Services.AddTransient<UnifiControllerManager>();*/

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<RadiusDisconnector>();
builder.Services.AddSingleton<RadiusAttributeParserService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddHostedService<RadiusAuthorizationBackgroundService>();
builder.Services.AddHostedService<RadiusAccountingBackgroundService>();
builder.Services.AddHostedService<DnsRedirectionServerBackgroundService>();

var app = builder.Build();

List<string> hostWhitelist = [
    "10.100.0.100"
];

app.Use(async (context, next) =>
{
    if (!hostWhitelist.Where(x => x.Equals(context.Request.Host.Host, StringComparison.InvariantCultureIgnoreCase)).Any())
    {
        context.Response.StatusCode = 302;
        context.Response.Headers["Location"] = "http://10.100.0.100/portal";
        return;
    }

    await next(context);
});

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
