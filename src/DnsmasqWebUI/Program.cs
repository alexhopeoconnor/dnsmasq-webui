using System.Net;
using DnsmasqWebUI.Components;
using DnsmasqWebUI.Extensions;
using DnsmasqWebUI.Http;
using DnsmasqWebUI.Http.Clients;
using DnsmasqWebUI.Options;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ---- Dnsmasq options (required paths validated at startup) ----
builder.Services.AddOptions<DnsmasqOptions>()
    .Bind(builder.Configuration.GetSection(DnsmasqOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<DnsmasqOptions>, DnsmasqOptionsValidator>();

// ---- Application services ----
builder.Services.AddApplicationServices();
builder.Services.AddHttpContextAccessor();

const string ApiClientName = "DnsmasqWebUI.Api";
builder.Services.AddHttpClient(ApiClientName, client =>
{
    client.BaseAddress = new Uri("http://localhost/", UriKind.Absolute);
})
.AddHttpMessageHandler<SameHostBaseAddressHandler>();

builder.Services.AddHttpClient<IStatusClient, StatusClient>(ApiClientName);
builder.Services.AddHttpClient<IConfigSetClient, ConfigSetClient>(ApiClientName);
builder.Services.AddHttpClient<IReloadClient, ReloadClient>(ApiClientName);
builder.Services.AddHttpClient<IHostsClient, HostsClient>(ApiClientName);
builder.Services.AddHttpClient<IDhcpHostsClient, DhcpHostsClient>(ApiClientName);
builder.Services.AddHttpClient<ILeasesClient, LeasesClient>(ApiClientName);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---- CORS (optional; Cors:Enabled in appsettings or env) ----
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

// ---- Fail fast: validate Dnsmasq options and exit with clear message if invalid ----
try
{
    _ = app.Services.GetRequiredService<IOptions<DnsmasqOptions>>().Value;
}
catch (OptionsValidationException ex)
{
    WriteValidationFailure(ex);
    Environment.Exit(1);
}

// ---- Middleware ----
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

if (builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
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
    app.UseCors(builder.Configuration.GetValue<string>("Cors:PolicyName") ?? "Default");

app.UseAntiforgery();

// ---- Endpoints ----
app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static void WriteValidationFailure(OptionsValidationException ex)
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
}
