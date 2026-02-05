using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET/PUT api/hosts.</summary>
public interface IHostsClient
{
    /// <summary>Gets managed hosts file entries from GET api/hosts.</summary>
    Task<IReadOnlyList<HostEntry>> GetHostsAsync(CancellationToken ct = default);

    /// <summary>Gets read-only hosts files (system + addn-hosts) from GET api/hosts/readonly.</summary>
    Task<IReadOnlyList<ReadOnlyHostsFile>> GetReadOnlyHostsAsync(CancellationToken ct = default);

    /// <summary>Writes managed hosts file and triggers reload via PUT api/hosts.</summary>
    Task<SaveWithReloadResult> SaveHostsAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default);
}
