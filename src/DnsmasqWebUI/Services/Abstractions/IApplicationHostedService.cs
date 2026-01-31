using Microsoft.Extensions.Hosting;

namespace DnsmasqWebUI.Services.Abstractions;

/// <summary>
/// Marker interface for application hosted services that are registered via assembly scanning.
/// Extends <see cref="IHostedService"/> so implementing types are registered with <c>AddHostedService&lt;T&gt;</c>.
/// </summary>
public interface IApplicationHostedService : IHostedService
{
}
