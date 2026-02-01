using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Client.Http.Abstractions;

/// <summary>Typed client for GET api/status.</summary>
public interface IStatusClient
{
    Task<DnsmasqServiceStatus> GetStatusAsync(CancellationToken ct = default);
}
