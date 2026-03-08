using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version.Abstractions;

/// <summary>Probes installed dnsmasq version and compares it to the configured minimum.</summary>
public interface IDnsmasqVersionService : IApplicationScopedService
{
    /// <summary>Runs the version command, parses output, and returns version info including support status.</summary>
    Task<DnsmasqVersionInfo> GetVersionInfoAsync(CancellationToken ct = default);
}
