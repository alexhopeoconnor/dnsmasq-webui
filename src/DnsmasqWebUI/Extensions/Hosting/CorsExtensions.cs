using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DnsmasqWebUI.Extensions.Hosting;

/// <summary>
/// CORS configuration from app settings (Cors:Enabled, Cors:PolicyName, Cors:AllowAnyOrigin, etc.).
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Adds CORS services and policy when Cors:Enabled is true. Policy name and rules read from configuration.
    /// </summary>
    public static IServiceCollection AddCorsFromConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Cors:Enabled"))
            return services;

        var policyName = configuration.GetValue<string>("Cors:PolicyName") ?? "Default";
        var allowAnyOrigin = configuration.GetValue<bool>("Cors:AllowAnyOrigin");
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var methods = configuration.GetSection("Cors:AllowedMethods").Get<string[]>();
        var headers = configuration.GetSection("Cors:AllowedHeaders").Get<string[]>();

        services.AddCors(options =>
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

        return services;
    }

    /// <summary>
    /// Uses CORS middleware when Cors:Enabled is true, with Cors:PolicyName.
    /// </summary>
    public static IApplicationBuilder UseCorsFromConfiguration(this IApplicationBuilder app, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Cors:Enabled"))
            return app;

        var policyName = configuration.GetValue<string>("Cors:PolicyName") ?? "Default";
        return app.UseCors(policyName);
    }
}
