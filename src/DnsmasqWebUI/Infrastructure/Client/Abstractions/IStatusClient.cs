using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET api/status.</summary>
public interface IStatusClient
{
    /// <summary>Gets dnsmasq and config status from GET api/status.</summary>
    Task<DnsmasqServiceStatus> GetStatusAsync(CancellationToken ct = default);
}
