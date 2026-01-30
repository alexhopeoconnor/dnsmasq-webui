using System.Reflection;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DnsmasqWebUI.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the assembly for types implementing <see cref="IApplicationScopedService"/>
    /// and registers each interface → implementation as scoped.
    /// Skips open generics; requires exactly one public implementation per interface.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var marker = typeof(IApplicationScopedService);

        // Only closed (non-generic) interfaces; open generics need typeof(IRepo&lt;&gt;, Repo&lt;&gt;) and are not handled here.
        var serviceInterfaces = assembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && t != marker && !t.IsGenericTypeDefinition && marker.IsAssignableFrom(t))
            .ToList();

        foreach (var iface in serviceInterfaces)
        {
            // Public, concrete, non-abstract, closed (no open generic classes).
            var implementations = assembly.GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract && !t.IsGenericTypeDefinition && iface.IsAssignableFrom(t))
                .ToList();

            if (implementations.Count == 0)
                continue;
            if (implementations.Count > 1)
                throw new InvalidOperationException(
                    $"Multiple implementations for {iface.FullName}: {string.Join(", ", implementations.Select(x => x.FullName))}. " +
                    "Register one explicitly or exclude the others from the scan.");

            services.AddScoped(iface, implementations[0]);
        }

        return services;
    }
}
