using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;

/// <summary>
/// Resolves the files and directories dnsmasq can use as hosts sources from an effective configuration.
/// This keeps source discovery, activation semantics, and diagnostics independent from the Hosts UI.
/// </summary>
public interface IHostSourceResolver : IApplicationSingleton
{
    /// <summary>
    /// Resolves configured system hosts, <c>addn-hosts</c> files/directories, <c>hostsdir</c>, and the managed hosts file.
    /// Paths are absolute and directory members are returned in dnsmasq's alphabetical order.
    /// </summary>
    IReadOnlyList<HostSource> Resolve(
        string? managedHostsPath,
        EffectiveDnsmasqConfig effectiveConfig,
        CancellationToken ct = default);
}
