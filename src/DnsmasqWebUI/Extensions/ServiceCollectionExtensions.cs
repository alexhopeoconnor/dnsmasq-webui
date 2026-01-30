using System.Reflection;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DnsmasqWebUI.Extensions;

public static class ServiceCollectionExtensions
{
    delegate void RegisterService(IServiceCollection s, Type iface, Type impl);

    static readonly (Type MarkerInterface, RegisterService Register)[] ApplicationRegistrations =
    [
        (typeof(IApplicationScopedService), (s, i, impl) => s.AddScoped(i, impl)),
        (typeof(IApplicationSingleton), (s, i, impl) => s.AddSingleton(i, impl)),
    ];

    /// <summary>
    /// Scans the assembly for types implementing application marker interfaces
    /// (<see cref="IApplicationScopedService"/>, <see cref="IApplicationSingleton"/>, etc.)
    /// and registers each interface → implementation with the configured lifetime.
    /// Skips open generics; requires exactly one public implementation per interface.
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
}
