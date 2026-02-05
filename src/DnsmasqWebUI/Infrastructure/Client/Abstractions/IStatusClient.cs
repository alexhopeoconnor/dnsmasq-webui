using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET api/status.</summary>
public interface IStatusClient
{
    Task<DnsmasqServiceStatus> GetStatusAsync(CancellationToken ct = default);
}
