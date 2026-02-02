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

    /// <summary>Leases path discovered from the config set (dhcp-leasefile= or dhcp-lease=; last wins). Null if main config missing or no directive found.</summary>
    public string? GetLeasesPath()
    {
        var set = GetConfigSet();
        if (set.Files.Count == 0)
            return null;
        var paths = set.Files.Select(f => f.Path).ToList();
        return DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(paths);
    }

    /// <summary>Effective config plus source per field so the UI can show "from X (readonly)" and why a flag cannot be unset.</summary>
    public (EffectiveDnsmasqConfig Config, EffectiveConfigSources Sources) GetEffectiveConfigWithSources()
    {
        var set = GetConfigSet();
        if (set.Files.Count == 0)
            return (CreateDefaultEffectiveConfig(), CreateDefaultEffectiveConfigSources());
        var paths = set.Files.Select(f => f.Path).ToList();
        var config = GetEffectiveConfig();
        var sources = BuildEffectiveConfigSources(paths, set.ManagedFilePath);
        return (config, sources);
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
        var serverLocal = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, new[] { "server", "local" });
        var addressValues = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "address");
        var interfaces = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "interface");
        var listenAddresses = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "listen-address");
        var exceptInterfaces = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "except-interface");
        var dhcpRanges = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "dhcp-range");
        var dhcpHostLines = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "dhcp-host");
        var dhcpOptionLines = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "dhcp-option");
        var resolvFiles = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "resolv-file");

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
            serverLocal, addressValues, interfaces, listenAddresses, exceptInterfaces, dhcpRanges, dhcpHostLines, dhcpOptionLines, resolvFiles,
            expandHosts, bogusPriv, strictOrder, noResolv, domainNeeded, noPoll, bindInterfaces, noNegcache, dhcpAuthoritative, leasefileRo,
            dhcpLeaseFilePath, cacheSize, port, localTtl, pidFilePath, user, group, logFacility, dhcpLeaseMax,
            negTtl, maxTtl, maxCacheTtl, minCacheTtl, dhcpTtl
        );
    }

    private static EffectiveDnsmasqConfig CreateDefaultEffectiveConfig() =>
        new(
            NoHosts: false, AddnHostsPaths: Array.Empty<string>(),
            ServerLocalValues: Array.Empty<string>(), AddressValues: Array.Empty<string>(), Interfaces: Array.Empty<string>(),
            ListenAddresses: Array.Empty<string>(), ExceptInterfaces: Array.Empty<string>(), DhcpRanges: Array.Empty<string>(),
            DhcpHostLines: Array.Empty<string>(), DhcpOptionLines: Array.Empty<string>(), ResolvFiles: Array.Empty<string>(),
            ExpandHosts: false, BogusPriv: false, StrictOrder: false, NoResolv: false, DomainNeeded: false, NoPoll: false,
            BindInterfaces: false, NoNegcache: false, DhcpAuthoritative: false, LeasefileRo: false,
            DhcpLeaseFilePath: null, CacheSize: null, Port: null, LocalTtl: null, PidFilePath: null, User: null, Group: null,
            LogFacility: null, DhcpLeaseMax: null, NegTtl: null, MaxTtl: null, MaxCacheTtl: null, MinCacheTtl: null, DhcpTtl: null
        );

    private static EffectiveConfigSources CreateDefaultEffectiveConfigSources() =>
        new(
            NoHosts: null, AddnHostsPaths: Array.Empty<PathWithSource>(),
            ServerLocalValues: Array.Empty<ValueWithSource>(), AddressValues: Array.Empty<ValueWithSource>(),
            Interfaces: Array.Empty<ValueWithSource>(), ListenAddresses: Array.Empty<ValueWithSource>(),
            ExceptInterfaces: Array.Empty<ValueWithSource>(), DhcpRanges: Array.Empty<ValueWithSource>(),
            DhcpHostLines: Array.Empty<ValueWithSource>(), DhcpOptionLines: Array.Empty<ValueWithSource>(),
            ResolvFiles: Array.Empty<ValueWithSource>(),
            ExpandHosts: null, BogusPriv: null, StrictOrder: null, NoResolv: null, DomainNeeded: null, NoPoll: null,
            BindInterfaces: null, NoNegcache: null, DhcpAuthoritative: null, LeasefileRo: null,
            DhcpLeaseFilePath: null, CacheSize: null, Port: null, LocalTtl: null, PidFilePath: null, User: null, Group: null,
            LogFacility: null, DhcpLeaseMax: null, NegTtl: null, MaxTtl: null, MaxCacheTtl: null, MinCacheTtl: null, DhcpTtl: null
        );

    private static EffectiveConfigSources BuildEffectiveConfigSources(IReadOnlyList<string> paths, string managedFilePath)
    {
        var (_, noHostsSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "no-hosts", managedFilePath);
        var addnHostsWithSource = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFilesWithSource(paths, managedFilePath);
        var serverLocalWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, new[] { "server", "local" }, managedFilePath);
        var addressWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, "address", managedFilePath);
        var interfacesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, "interface", managedFilePath);
        var listenAddressesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, "listen-address", managedFilePath);
        var exceptInterfacesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, "except-interface", managedFilePath);
        var dhcpRangesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, "dhcp-range", managedFilePath);
        var dhcpHostLinesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, "dhcp-host", managedFilePath);
        var dhcpOptionLinesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, "dhcp-option", managedFilePath);
        var resolvFilesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, "resolv-file", managedFilePath);

        var (_, expandHostsSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "expand-hosts", managedFilePath);
        var (_, bogusPrivSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "bogus-priv", managedFilePath);
        var (_, strictOrderSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "strict-order", managedFilePath);
        var (_, noResolvSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "no-resolv", managedFilePath);
        var (_, domainNeededSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "domain-needed", managedFilePath);
        var (_, noPollSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "no-poll", managedFilePath);
        var (_, bindInterfacesSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "bind-interfaces", managedFilePath);
        var (_, noNegcacheSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "no-negcache", managedFilePath);
        var (_, dhcpAuthoritativeSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "dhcp-authoritative", managedFilePath);
        var (_, leasefileRoSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, "leasefile-ro", managedFilePath);

        var (_, dhcpLeaseFilePathSource) = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFilesWithSource(paths, managedFilePath);
        var (_, cacheSizeSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "cache-size", managedFilePath);
        var (_, portSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "port", managedFilePath);
        var (_, localTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "local-ttl", managedFilePath);
        var (_, pidFilePathSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "pid-file", managedFilePath);
        var (_, userSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "user", managedFilePath);
        var (_, groupSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "group", managedFilePath);
        var (_, logFacilitySource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "log-facility", managedFilePath);
        var (_, dhcpLeaseMaxSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "dhcp-lease-max", managedFilePath);
        var (_, negTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "neg-ttl", managedFilePath);
        var (_, maxTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "max-ttl", managedFilePath);
        var (_, maxCacheTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "max-cache-ttl", managedFilePath);
        var (_, minCacheTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "min-cache-ttl", managedFilePath);
        var (_, dhcpTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, "dhcp-ttl", managedFilePath);

        return new EffectiveConfigSources(
            noHostsSource, addnHostsWithSource.Select(t => new PathWithSource(t.Path, t.Source)).ToList(),
            serverLocalWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            addressWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            interfacesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            listenAddressesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            exceptInterfacesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpRangesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpHostLinesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpOptionLinesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            resolvFilesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            expandHostsSource, bogusPrivSource, strictOrderSource, noResolvSource, domainNeededSource, noPollSource,
            bindInterfacesSource, noNegcacheSource, dhcpAuthoritativeSource, leasefileRoSource,
            dhcpLeaseFilePathSource, cacheSizeSource, portSource, localTtlSource, pidFilePathSource, userSource, groupSource,
            logFacilitySource, dhcpLeaseMaxSource, negTtlSource, maxTtlSource, maxCacheTtlSource, minCacheTtlSource, dhcpTtlSource
        );
    }

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
