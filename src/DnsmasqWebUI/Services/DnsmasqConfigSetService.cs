using DnsmasqWebUI.Models;
using DnsmasqWebUI.Configuration;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

public class DnsmasqConfigSetService : IDnsmasqConfigSetService
{
    private readonly DnsmasqOptions _options;

    public DnsmasqConfigSetService(IOptions<DnsmasqOptions> options)
    {
        _options = options.Value;
    }

    public Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default) =>
        Task.FromResult(GetConfigSet());

    /// <summary>Leases path discovered from the config set (dhcp-leasefile= or dhcp-lease-file=; last wins). Null if main config missing or no directive found.</summary>
    public string? GetLeasesPath()
    {
        var set = GetConfigSet();
        if (set.Files.Count == 0)
            return null;
        var paths = set.Files.Select(f => f.Path).ToList();
        return DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(paths);
    }

    /// <summary>Effective config from the config set (single-value and flag options; last/any wins).</summary>
    public EffectiveDnsmasqConfig GetEffectiveConfig()
    {
        var set = GetConfigSet();
        if (set.Files.Count == 0)
            return CreateDefaultEffectiveConfig();
        var paths = set.Files.Select(f => f.Path).ToList();

        var noHosts = DnsmasqConfIncludeParser.GetNoHostsFromConfigFiles(paths);
        var addnHosts = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(paths);

        var expandHosts = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "expand-hosts");
        var bogusPriv = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "bogus-priv");
        var strictOrder = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "strict-order");
        var noResolv = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "no-resolv");
        var domainNeeded = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "domain-needed");
        var noPoll = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "no-poll");
        var bindInterfaces = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "bind-interfaces");
        var noNegcache = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "no-negcache");
        var dhcpAuthoritative = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "dhcp-authoritative");
        var leasefileRo = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, "leasefile-ro");

        var dhcpLeaseFilePath = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(paths);

        var (cacheVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "cache-size");
        var cacheSize = TryParseInt(cacheVal);

        var (portVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "port");
        var port = TryParseInt(portVal);

        var (localTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "local-ttl");
        var localTtl = TryParseInt(localTtlVal);

        var (pidVal, pidDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "pid-file");
        var pidFilePath = DnsmasqConfIncludeParser.ResolvePath(pidVal, pidDir);

        var (userVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "user");
        var user = string.IsNullOrWhiteSpace(userVal) ? null : userVal.Trim();

        var (groupVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "group");
        var group = string.IsNullOrWhiteSpace(groupVal) ? null : groupVal.Trim();

        var (logFacVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "log-facility");
        var logFacility = string.IsNullOrWhiteSpace(logFacVal) ? null : logFacVal.Trim();

        var (leaseMaxVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "dhcp-lease-max");
        var dhcpLeaseMax = TryParseInt(leaseMaxVal);

        var (negTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "neg-ttl");
        var negTtl = TryParseInt(negTtlVal);

        var (maxTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "max-ttl");
        var maxTtl = TryParseInt(maxTtlVal);

        var (maxCacheTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "max-cache-ttl");
        var maxCacheTtl = TryParseInt(maxCacheTtlVal);

        var (minCacheTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "min-cache-ttl");
        var minCacheTtl = TryParseInt(minCacheTtlVal);

        var (dhcpTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "dhcp-ttl");
        var dhcpTtl = TryParseInt(dhcpTtlVal);

        return new EffectiveDnsmasqConfig(
            noHosts, addnHosts,
            expandHosts, bogusPriv, strictOrder, noResolv, domainNeeded, noPoll, bindInterfaces, noNegcache, dhcpAuthoritative, leasefileRo,
            dhcpLeaseFilePath, cacheSize, port, localTtl, pidFilePath, user, group, logFacility, dhcpLeaseMax,
            negTtl, maxTtl, maxCacheTtl, minCacheTtl, dhcpTtl
        );
    }

    private static EffectiveDnsmasqConfig CreateDefaultEffectiveConfig() =>
        new(
            NoHosts: false, AddnHostsPaths: Array.Empty<string>(),
            ExpandHosts: false, BogusPriv: false, StrictOrder: false, NoResolv: false, DomainNeeded: false, NoPoll: false,
            BindInterfaces: false, NoNegcache: false, DhcpAuthoritative: false, LeasefileRo: false,
            DhcpLeaseFilePath: null, CacheSize: null, Port: null, LocalTtl: null, PidFilePath: null, User: null, Group: null,
            LogFacility: null, DhcpLeaseMax: null, NegTtl: null, MaxTtl: null, MaxCacheTtl: null, MinCacheTtl: null, DhcpTtl: null
        );

    private static int? TryParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return int.TryParse(value.Trim(), out var n) ? n : null;
    }

    /// <summary>Additional hosts paths discovered from the config set (addn-hosts=; cumulative). Empty list if main config missing or no addn-hosts.</summary>
    public IReadOnlyList<string> GetAddnHostsPaths()
    {
        return GetEffectiveConfig().AddnHostsPaths;
    }

    /// <inheritdoc />
    public (string? Start, string? End) GetDhcpRange()
    {
        var set = GetConfigSet();
        if (set.Files.Count == 0)
            return (null, null);
        var paths = set.Files.Select(f => f.Path).ToList();
        var (raw, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "dhcp-range");
        return ParseDhcpRangeStartEnd(raw);
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

    private DnsmasqConfigSet GetConfigSet()
    {
        var mainPath = _options.MainConfigPath;
        if (string.IsNullOrEmpty(mainPath))
            return new DnsmasqConfigSet("", "", Array.Empty<DnsmasqConfigSetEntry>());

        var mainFull = Path.GetFullPath(mainPath);
        var mainDir = Path.GetDirectoryName(mainFull) ?? "";
        var managedFilePath = Path.Combine(mainDir, _options.ManagedFileName);

        var withSource = DnsmasqConfIncludeParser.GetIncludedPathsWithSource(mainPath);
        var files = withSource.Select(p => new DnsmasqConfigSetEntry(
            p.Path,
            Path.GetFileName(p.Path),
            p.Source,
            IsManaged: string.Equals(p.Path, managedFilePath, StringComparison.Ordinal)
        )).ToList();

        if (files.All(e => !string.Equals(e.Path, managedFilePath, StringComparison.Ordinal)))
            files.Add(new DnsmasqConfigSetEntry(managedFilePath, Path.GetFileName(managedFilePath), DnsmasqConfFileSource.ConfFile, IsManaged: true));

        return new DnsmasqConfigSet(mainFull, managedFilePath, files);
    }
}
