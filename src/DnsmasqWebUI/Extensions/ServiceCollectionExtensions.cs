using System.Reflection;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DnsmasqWebUI.Extensions;

public static class ServiceCollectionExtensions
{
    delegate void RegisterService(IServiceCollection s, Type iface, Type impl);

    static readonly (Type MarkerInterface, RegisterService Register)[] ApplicationRegistrations =
    [
        (typeof(IApplicationScopedService), (s, i, impl) => s.AddScoped(i, impl)),
        (typeof(IApplicationSingleton), (s, i, impl) => s.AddSingleton(i, impl)),
        (typeof(IApplicationHostedService), (s, i, impl) => AddHostedServiceConcrete(s, impl)),
    ];

    /// <summary>
    /// Scans the assembly for types implementing application marker interfaces
    /// (<see cref="IApplicationScopedService"/>, <see cref="IApplicationSingleton"/>, <see cref="IApplicationHostedService"/>, etc.)
    /// and registers each with the configured lifetime. Hosted services are registered via <c>AddHostedService&lt;T&gt;</c>.
    /// Skips open generics; requires exactly one public implementation per interface (scoped/singleton).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        foreach (var (markerInterface, register) in ApplicationRegistrations)
            ScanAndRegister(services, markerInterface, register);
        return services;
    }

    static void ScanAndRegister(
        IServiceCollection services,
        Type markerInterface,
        RegisterService register)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var serviceInterfaces = assembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && t != markerInterface && !t.IsGenericTypeDefinition && markerInterface.IsAssignableFrom(t))
            .ToList();

        if (serviceInterfaces.Count > 0)
        {
            foreach (var iface in serviceInterfaces)
            {
                var implementations = assembly.GetTypes()
                    .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract && !t.IsGenericTypeDefinition && iface.IsAssignableFrom(t))
                    .ToList();

                if (implementations.Count == 0)
                    continue;
                if (implementations.Count > 1)
                    throw new InvalidOperationException(
                        $"Multiple implementations for {iface.FullName}: {string.Join(", ", implementations.Select(x => x.FullName))}. " +
                        "Register one explicitly or exclude the others from the scan.");

                register(services, iface, implementations[0]);
            }
        }
        else
        {
            // Marker has no derived interfaces (e.g. IApplicationHostedService): register each class that implements the marker.
            var implementations = assembly.GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract && !t.IsGenericTypeDefinition && markerInterface.IsAssignableFrom(t))
                .ToList();

            foreach (var impl in implementations)
                register(services, markerInterface, impl);
        }
    }

    /// <summary>Registers a hosted service by its concrete type so each gets a distinct registration (AddHostedService&lt;T&gt;).</summary>
    static void AddHostedServiceConcrete(IServiceCollection services, Type implementationType)
    {
        var method = typeof(ServiceCollectionHostedServiceExtensions)
            .GetMethods()
            .First(m => m.Name == "AddHostedService" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(IServiceCollection));
        method.MakeGenericMethod(implementationType).Invoke(null, [services]);
    }
}
