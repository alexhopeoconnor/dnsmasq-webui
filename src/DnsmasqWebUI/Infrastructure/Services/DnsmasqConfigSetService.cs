using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services;

public class DnsmasqConfigSetService : IDnsmasqConfigSetService
{
    private readonly IConfigSetCache _cache;

    public DnsmasqConfigSetService(IConfigSetCache cache)
    {
        _cache = cache;
    }

    public async Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default)
    {
        var snapshot = await _cache.GetSnapshotAsync(ct);
        return snapshot.Set;
    }

    /// <summary>Leases path discovered from the config set (dhcp-leasefile= or dhcp-lease=; last wins). Null if main config missing or no directive found.</summary>
    public string? GetLeasesPath()
    {
        return GetSnapshot().Config.DhcpLeaseFilePath;
    }

    /// <summary>Effective config plus source per field so the UI can show "from X (readonly)" and why a flag cannot be unset.</summary>
    public (EffectiveDnsmasqConfig Config, EffectiveConfigSources Sources) GetEffectiveConfigWithSources()
    {
        var snapshot = GetSnapshot();
        return (snapshot.Config, snapshot.Sources);
    }

    /// <summary>Effective config from the config set (single-value and flag options; last/any wins).</summary>
    public EffectiveDnsmasqConfig GetEffectiveConfig()
    {
        return GetSnapshot().Config;
    }

    /// <summary>Additional hosts paths discovered from the config set (addn-hosts=; cumulative). Empty list if main config missing or no addn-hosts.</summary>
    public IReadOnlyList<string> GetAddnHostsPaths()
    {
        return GetSnapshot().Config.AddnHostsPaths;
    }

    /// <inheritdoc />
    public (string? Start, string? End) GetDhcpRange()
    {
        var snapshot = GetSnapshot();
        var ranges = snapshot.Config.DhcpRanges;
        var last = ranges.Count > 0 ? ranges[ranges.Count - 1] : null;
        return ParseDhcpRangeStartEnd(last);
    }

    /// <summary>Parses dhcp-range value to (startIp, endIp). Format is typically start,end,mask,lease or tag:...,start,end,...; finds first two IPv4-looking tokens.</summary>
    internal static (string? Start, string? End) ParseDhcpRangeStartEnd(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (null, null);
        var parts = raw.Split(',');
        string? start = null;
        string? end = null;
        foreach (var p in parts)
        {
            var t = p.Trim();
            if (string.IsNullOrEmpty(t)) continue;
            if (!System.Net.IPAddress.TryParse(t, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                continue;
            if (start == null) { start = t; continue; }
            if (end == null) { end = t; break; }
        }
        return (start, end);
    }

    private ConfigSetSnapshot GetSnapshot() =>
        _cache.GetSnapshotAsync(CancellationToken.None).GetAwaiter().GetResult();
}
