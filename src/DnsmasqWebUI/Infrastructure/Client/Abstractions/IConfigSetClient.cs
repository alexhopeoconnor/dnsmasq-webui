using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET api/config/set.</summary>
public interface IConfigSetClient
{
    /// <summary>Gets the config set (main + conf-file/conf-dir) and managed file path from GET api/config/set.</summary>
    Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default);
}
