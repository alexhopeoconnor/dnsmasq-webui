using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;

/// <summary>Discovers the dnsmasq config set (main + conf-file/conf-dir) and the managed file path.</summary>
public interface IDnsmasqConfigSetService : IApplicationSingleton
{
    Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default);

    /// <summary>Leases path discovered from the config set (dhcp-leasefile= / dhcp-lease=; last wins). Null if not found.</summary>
    string? GetLeasesPath();

    /// <summary>Effective hosts-related config after reading all config files: no-hosts flag and addn-hosts= paths (cumulative).</summary>
    EffectiveDnsmasqConfig GetEffectiveConfig();

    /// <summary>Effective config plus source per field so the UI can show exactly where each value came from (file path, readonly reason).</summary>
    (EffectiveDnsmasqConfig Config, EffectiveConfigSources Sources) GetEffectiveConfigWithSources();

    /// <summary>Additional hosts paths discovered from the config set (addn-hosts=; cumulative, all in order). Empty if none.</summary>
    IReadOnlyList<string> GetAddnHostsPaths();

    /// <summary>Start and end IP of the last dhcp-range= (e.g. 172.28.0.10,172.28.0.50). Parsed from the raw value; (null, null) when not set or unparseable.</summary>
    (string? Start, string? End) GetDhcpRange();
}
