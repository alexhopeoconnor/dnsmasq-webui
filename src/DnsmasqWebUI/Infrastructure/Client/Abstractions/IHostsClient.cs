using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET/PUT api/hosts.</summary>
public interface IHostsClient
{
    Task<IReadOnlyList<HostEntry>> GetHostsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReadOnlyHostsFile>> GetReadOnlyHostsAsync(CancellationToken ct = default);
    Task<SaveWithReloadResult> SaveHostsAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default);
}
