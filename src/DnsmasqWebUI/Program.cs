using DnsmasqWebUI.Components;
using DnsmasqWebUI.Extensions;
using DnsmasqWebUI.Options;

// CreateBuilder(args) loads config in order: appsettings.json, appsettings.{Environment}.json,
// for env vars; override Dnsmasq options via e.g. Dnsmasq__ReloadCommand= or --Dnsmasq:ReloadCommand=.
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DnsmasqOptions>(
    builder.Configuration.GetSection(DnsmasqOptions.SectionName));

builder.Services.AddApplicationServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
{
    var context = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var baseUri = context != null ? $"{context.Request.Scheme}://{context.Request.Host}" : "http://localhost";
    return new HttpClient { BaseAddress = new Uri(baseUri) };
});

// Same app hosts both:
// - API: AddControllers() + MapControllers() → routes like /api/status, /api/hosts, /api/reload.
// - Blazor: AddRazorComponents + AddInteractiveServerComponents + MapRazorComponents<App>() → pages like /, /hosts, /dhcp, /leases.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

// Map API controllers first, then static assets, then Blazor (catch-all for SPA-style routes).
app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
