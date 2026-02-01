using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace DnsmasqWebUI.Extensions;

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
            KnownNetworks = { new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Loopback, 8) },
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
}
