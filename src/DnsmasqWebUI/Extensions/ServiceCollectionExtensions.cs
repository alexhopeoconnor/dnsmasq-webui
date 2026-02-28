using System.Reflection;
using DnsmasqWebUI.Infrastructure.Client;
using DnsmasqWebUI.Infrastructure.Client.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DnsmasqWebUI.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Name of the HttpClient used for same-host API calls (status, config, reload, hosts, etc.).</summary>
    public const string DnsmasqApiClientName = "DnsmasqWebUI.Api";

    /// <summary>Name of the HttpClient used for GitHub API (e.g. latest release check).</summary>
    public const string GitHubClientName = "DnsmasqWebUI.GitHub";

    /// <summary>
    /// Registers the named HttpClient for same-host API calls and all typed API clients
    /// (IStatusClient, IConfigSetClient, IReloadClient, IHostsClient, IDhcpHostsClient, ILeasesClient).
    /// </summary>
    public static IServiceCollection AddDnsmasqApiHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(DnsmasqApiClientName, client =>
        {
            client.BaseAddress = new Uri("http://localhost/", UriKind.Absolute);
        })
        .AddHttpMessageHandler<SameHostBaseAddressHandler>();

        services.AddHttpClient<IStatusClient, StatusClient>(DnsmasqApiClientName);
        services.AddHttpClient<IConfigSetClient, ConfigSetClient>(DnsmasqApiClientName);
        services.AddHttpClient<IReloadClient, ReloadClient>(DnsmasqApiClientName);
        services.AddHttpClient<IHostsClient, HostsClient>(DnsmasqApiClientName);
        services.AddHttpClient<IDhcpHostsClient, DhcpHostsClient>(DnsmasqApiClientName);
        services.AddHttpClient<ILeasesClient, LeasesClient>(DnsmasqApiClientName);
        services.AddHttpClient<ILoggingClient, LoggingClient>(DnsmasqApiClientName);

        services.AddHttpClient(GitHubClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/", UriKind.Absolute);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "dnsmasq-webui");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github.v3+json");
        });

        return services;
    }

    delegate void RegisterService(IServiceCollection s, Type iface, Type impl);

    static readonly (Type MarkerInterface, RegisterService Register)[] ApplicationRegistrations =
    [
        (typeof(IApplicationScopedService), (s, i, impl) => s.AddScoped(i, impl)),
        (typeof(IApplicationScopedConcrete), (s, i, impl) => s.AddScoped(impl, impl)),
        (typeof(IApplicationSingleton), (s, i, impl) => s.AddSingleton(i, impl)),
        (typeof(IApplicationHostedService), (s, i, impl) => AddHostedServiceConcrete(s, impl)),
    ];

    /// <summary>
    /// Scans the assembly for types implementing application marker interfaces
    /// (<see cref="IApplicationScopedService"/>, <see cref="IApplicationScopedConcrete"/>, <see cref="IApplicationSingleton"/>, <see cref="IApplicationHostedService"/>, etc.)
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
