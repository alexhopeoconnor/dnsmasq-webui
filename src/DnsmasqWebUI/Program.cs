using DnsmasqWebUI.Components;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Extensions;
using DnsmasqWebUI.Infrastructure.Helpers.Http;
using Microsoft.Extensions.Options;

// When not in Development, use the app's directory (not CWD) so static assets work when run via symlink or from any CWD.
var isDevelopment = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
var builder = isDevelopment
    ? WebApplication.CreateBuilder(args)
    : WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Args = args });

// ---- Application options (title, etc.) ----
builder.Services.AddOptions<ApplicationOptions>()
    .Bind(builder.Configuration.GetSection(ApplicationOptions.SectionName));

// ---- Dnsmasq options (required paths validated at startup) ----
builder.Services.AddOptions<DnsmasqOptions>()
    .Bind(builder.Configuration.GetSection(DnsmasqOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<DnsmasqOptions>, DnsmasqOptionsValidator>();

// ---- Application services ----
builder.Services.AddApplicationServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDnsmasqApiHttpClients();

builder.Services.AddControllers()
    .AddJsonOptions(o => ApiJsonOptions.ConfigureServer(o.JsonSerializerOptions));
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCorsFromConfiguration(builder.Configuration);

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

app.UseForwardedHeadersFromConfiguration(builder.Configuration);
app.UseHttpsFromConfiguration(builder.Configuration);
app.UseCorsFromConfiguration(builder.Configuration);
app.UseAntiforgery();

// ---- Endpoints ----
app.MapControllers();
// UseStaticFiles: MapStaticAssets returns 0-byte responses for fingerprinted assets (known bug). Serve from wwwroot directly.
app.UseStaticFiles();
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
