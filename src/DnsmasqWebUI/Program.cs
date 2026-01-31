using System.Net;
using DnsmasqWebUI.Components;
using DnsmasqWebUI.Extensions;
using DnsmasqWebUI.Options;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

// CreateBuilder(args) loads config in order: appsettings.json, appsettings.{Environment}.json,
// env vars; override via e.g. Dnsmasq__ReloadCommand= or ForwardedHeaders__Enabled=true.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<DnsmasqOptions>()
    .Bind(builder.Configuration.GetSection(DnsmasqOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<DnsmasqOptions>, DnsmasqOptionsValidator>();

builder.Services.AddApplicationServices();

builder.Services.AddHttpContextAccessor();
// Same-host HttpClient via IHttpClientFactory: relative URIs (e.g. /api/status) are rewritten to the
// current request's scheme/host/path base by SameHostBaseAddressHandler. Components keep @inject HttpClient.
// Options.DefaultName is the "default" client name (empty string) used when you inject HttpClient or call CreateClient() with no name.
builder.Services.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName)
    .AddHttpMessageHandler<DnsmasqWebUI.Http.SameHostBaseAddressHandler>();

// Same app hosts both:
// - API: AddControllers() + MapControllers() → routes like /api/status, /api/hosts, /api/reload.
// - Blazor: AddRazorComponents + AddInteractiveServerComponents + MapRazorComponents<App>() → pages like /, /hosts, /dhcp, /leases.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// CORS: only added when Cors:Enabled is true. Configure origins/methods/headers in appsettings or
// env (e.g. Cors__Enabled=true, Cors__AllowedOrigins__0=https://app.example.com).
var corsEnabled = builder.Configuration.GetValue<bool>("Cors:Enabled");
if (corsEnabled)
{
    var policyName = builder.Configuration.GetValue<string>("Cors:PolicyName") ?? "Default";
    var allowAnyOrigin = builder.Configuration.GetValue<bool>("Cors:AllowAnyOrigin");
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    var methods = builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>();
    var headers = builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(policyName, policy =>
        {
            if (allowAnyOrigin)
                policy.AllowAnyOrigin();
            else if (origins.Length > 0)
                policy.WithOrigins(origins);
            if (methods is { Length: > 0 })
                policy.WithMethods(methods);
            else
                policy.AllowAnyMethod();
            if (headers is { Length: > 0 })
                policy.WithHeaders(headers);
            else
                policy.AllowAnyHeader();
        });
    });
}

var app = builder.Build();

// Trigger Dnsmasq options validation and print a clear error to stderr if it fails (before any stack trace).
try
{
    _ = app.Services.GetRequiredService<IOptions<DnsmasqOptions>>().Value;
}
catch (OptionsValidationException ex)
{
    var err = Console.Error;
    err.WriteLine();
    err.WriteLine("*** DNSMASQ-WEBUI CONFIGURATION ERROR ***");
    err.WriteLine();
    foreach (var failure in ex.Failures)
        err.WriteLine("  • " + failure);
    err.WriteLine();
    err.WriteLine("Fix the configuration (appsettings.json or Dnsmasq__* environment variables) and restart.");
    err.WriteLine();
    Environment.Exit(1);
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

// HTTPS: Kestrel can serve HTTPS via config (no code needed). Set ASPNETCORE_URLS=https://*:5001 and
// ASPNETCORE_Kestrel__Certificates__Default__Path (and __Password) for the cert. When behind a
// reverse proxy that terminates TLS, set ForwardedHeaders__Enabled=true so X-Forwarded-Proto is used;
// set Https__UseRedirectAndHsts=true to redirect HTTP→HTTPS and send HSTS. Both default false.
var forwardedEnabled = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
if (forwardedEnabled)
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        KnownNetworks = { new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Loopback, 8) },
    });
}

if (builder.Configuration.GetValue<bool>("Https:UseRedirectAndHsts"))
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

if (corsEnabled)
{
    var policyName = builder.Configuration.GetValue<string>("Cors:PolicyName") ?? "Default";
    app.UseCors(policyName);
}

app.UseAntiforgery();

// Map API controllers first, then static assets, then Blazor (catch-all for SPA-style routes).
app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
