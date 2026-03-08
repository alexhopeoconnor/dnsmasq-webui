using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DnsmasqWebUI.Extensions.Hosting;

/// <summary>
/// Middleware configuration from app settings (forwarded headers, HTTPS, CORS).
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Uses forwarded headers (X-Forwarded-For, X-Forwarded-Proto) when ForwardedHeaders:Enabled is true.
    /// </summary>
    public static IApplicationBuilder UseForwardedHeadersFromConfiguration(this IApplicationBuilder app, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
            return app;

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            KnownIPNetworks = { new System.Net.IPNetwork(IPAddress.Loopback, 8) },
        });

        return app;
    }

    /// <summary>
    /// Uses HTTPS redirection and HSTS when Https:UseRedirectAndHsts is true.
    /// </summary>
    public static IApplicationBuilder UseHttpsFromConfiguration(this IApplicationBuilder app, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Https:UseRedirectAndHsts"))
            return app;

        app.UseHttpsRedirection();
        app.UseHsts();

        return app;
    }

    /// <summary>
    /// Maps the readiness health check at /healthz/ready (checks with tag "ready", returns JSON status).
    /// </summary>
    public static IEndpointRouteBuilder MapReadyHealthCheck(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = static async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var status = report.Status == HealthStatus.Healthy ? "ok" : "unhealthy";
                await context.Response.WriteAsync($"{{\"status\":\"{status}\"}}", context.RequestAborted);
            }
        });
        return endpoints;
    }
}
