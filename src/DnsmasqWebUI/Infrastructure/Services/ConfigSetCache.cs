using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Parsers;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services;

/// <summary>
/// Singleton cache for the config set (main + conf-dir + managed file). Reads all files once per refresh,
/// builds snapshot from pathToLines via parser overloads. Invalidates on watchers (main + managed file), staleness, or Invalidate.
/// Self-write: NotifyWeWroteManagedConfig updates cached managed content in place and ignores the next managed-file watcher event for a short window.
/// </summary>
public sealed class ConfigSetCache : IConfigSetCache, IDisposable
{
    private const int StaleCacheSeconds = 120;
    private const double SelfWriteIgnoreSeconds = 1.5;

    private readonly DnsmasqOptions _options;
    private readonly ILogger<ConfigSetCache> _logger;
    private readonly object _lock = new();
    private ConfigSetSnapshot? _snapshot;
    private DateTime? _lastReadUtc;
    private bool _dirty = true;
    private DateTime _lastWriteManagedUtc = DateTime.MinValue;
    private FileSystemWatcher? _watcherMain;
    private FileSystemWatcher? _watcherManaged;

    public ConfigSetCache(IOptions<DnsmasqOptions> options, ILogger<ConfigSetCache> logger)
    {
        _options = options.Value;
        _logger = logger;
        var (mainPath, managedPath) = GetPaths();
        if (!string.IsNullOrEmpty(mainPath))
            TryAddWatcher(Path.GetDirectoryName(mainPath), Path.GetFileName(mainPath), ref _watcherMain, "main config");
        if (!string.IsNullOrEmpty(managedPath))
            TryAddWatcher(Path.GetDirectoryName(managedPath), Path.GetFileName(managedPath), ref _watcherManaged, "managed config");
    }

    private (string? MainPath, string? ManagedPath) GetPaths()
    {
        var mainPath = _options.MainConfigPath;
        if (string.IsNullOrEmpty(mainPath))
            return (null, null);
        var mainFull = Path.GetFullPath(mainPath);
        var mainDir = Path.GetDirectoryName(mainFull) ?? "";
        var managedPath = Path.Combine(mainDir, _options.ManagedFileName);
        return (mainFull, managedPath);
    }

    private void TryAddWatcher(string? dir, string? fileName, ref FileSystemWatcher? watcher, string label)
    {
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(fileName))
            return;
        try
        {
            if (!Directory.Exists(dir))
            {
                _logger.LogDebug("{Label} directory does not exist yet: {Dir}", label, dir);
                return;
            }
            watcher = new FileSystemWatcher(dir)
            {
                Filter = fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.EnableRaisingEvents = true;
            _logger.LogDebug("Watching {Label}: {Path}", label, Path.Combine(dir, fileName));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create file watcher for {Label}: {Dir}", label, dir);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var fullPath = Path.GetFullPath(e.FullPath);
        var (_, managedPath) = GetPaths();
        if (!string.IsNullOrEmpty(managedPath) && string.Equals(fullPath, Path.GetFullPath(managedPath), StringComparison.Ordinal))
        {
            lock (_lock)
            {
                var elapsed = (DateTime.UtcNow - _lastWriteManagedUtc).TotalSeconds;
                if (elapsed < SelfWriteIgnoreSeconds)
                    return;
            }
        }
        lock (_lock)
            _dirty = true;
    }

    public void Invalidate()
    {
        lock (_lock)
            _dirty = true;
    }

    public void NotifyWeWroteManagedConfig(ManagedConfigContent newContent)
    {
        lock (_lock)
        {
            _lastWriteManagedUtc = DateTime.UtcNow;
            _dirty = true;
        }
    }

    public async Task<ConfigSetSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        return await Task.Run(() => GetSnapshot(ct), ct);
    }

    private ConfigSetSnapshot GetSnapshot(CancellationToken ct)
    {
        var (mainFull, managedPath) = GetPaths();
        if (string.IsNullOrEmpty(mainFull))
            return CreateDefaultSnapshot();

        lock (_lock)
        {
            if (!_dirty && _snapshot != null && _lastReadUtc.HasValue)
            {
                var ageSeconds = (DateTime.UtcNow - _lastReadUtc.Value).TotalSeconds;
                if (ageSeconds < StaleCacheSeconds)
                    return _snapshot;
                _dirty = true;
            }

            var managedHostsFileName = (_options.ManagedHostsFileName ?? "").Trim();
            if (string.IsNullOrEmpty(managedHostsFileName))
                managedHostsFileName = "zz-dnsmasq-webui.hosts";
            var mainDir = Path.GetDirectoryName(mainFull) ?? "";
            var managedHostsFilePath = Path.Combine(mainDir, managedHostsFileName);
            var set = BuildConfigSet(mainFull, managedPath, managedHostsFilePath);
            if (set.Files.Count == 0)
            {
                _snapshot = CreateDefaultSnapshot();
                _lastReadUtc = DateTime.UtcNow;
                _dirty = false;
                return _snapshot;
            }

            var paths = set.Files.Select(f => f.Path).ToList();
            var pathToLines = ReadAllPaths(paths, ct);
            var config = BuildEffectiveConfig(paths, pathToLines);
            var sources = BuildEffectiveConfigSources(paths, pathToLines, set.ManagedFilePath, set.ManagedHostsFilePath);
            var managedContent = BuildManagedContent(pathToLines, set.ManagedFilePath);
            var dhcpHostEntries = BuildDhcpHostEntries(set, pathToLines);

            _snapshot = new ConfigSetSnapshot(set, config, sources, managedContent, dhcpHostEntries);
            _lastReadUtc = DateTime.UtcNow;
            _dirty = false;
            return _snapshot;
        }
    }

    private static ConfigSetSnapshot CreateDefaultSnapshot()
    {
        var set = new DnsmasqConfigSet("", "", null, Array.Empty<DnsmasqConfigSetEntry>());
        var config = CreateDefaultEffectiveConfig();
        var sources = CreateDefaultEffectiveConfigSources();
        var managedContent = new ManagedConfigContent(Array.Empty<DnsmasqConfLine>(), "");
        return new ConfigSetSnapshot(set, config, sources, managedContent, Array.Empty<DhcpHostEntry>());
    }

    private static DnsmasqConfigSet BuildConfigSet(string mainFull, string? managedFilePath, string managedHostsFilePath)
    {
        var mainPath = mainFull;
        var mainDir = Path.GetDirectoryName(mainFull) ?? "";

        var withSource = DnsmasqConfIncludeParser.GetIncludedPathsWithSource(mainPath);
        var files = withSource.Select(p => new DnsmasqConfigSetEntry(
            p.Path,
            Path.GetFileName(p.Path),
            p.Source,
            IsManaged: string.Equals(p.Path, managedFilePath, StringComparison.Ordinal)
        )).ToList();

        if (managedFilePath != null && files.All(e => !string.Equals(e.Path, managedFilePath, StringComparison.Ordinal)))
            files.Add(new DnsmasqConfigSetEntry(managedFilePath, Path.GetFileName(managedFilePath), DnsmasqConfFileSource.ConfFile, IsManaged: true));

        return new DnsmasqConfigSet(mainFull, managedFilePath ?? "", managedHostsFilePath, files);
    }

    private static IReadOnlyDictionary<string, string[]> ReadAllPaths(IReadOnlyList<string> paths, CancellationToken ct)
    {
        var dict = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            ct.ThrowIfCancellationRequested();
            var canonical = Path.GetFullPath(path);
            if (dict.ContainsKey(canonical))
                continue;
            if (!File.Exists(path))
            {
                dict[canonical] = Array.Empty<string>();
                continue;
            }
            try
            {
                dict[canonical] = DnsmasqFileEncoding.ReadConfigLines(path);
            }
            catch
            {
                dict[canonical] = Array.Empty<string>();
            }
        }
        return dict;
    }

    /// <summary>Dispatches to GetFlag, GetLastValue, or GetMultiValue based on <see cref="EffectiveConfigParserBehaviorMap"/>.</summary>
    private static object ParseOptionValue(IReadOnlyList<string> paths, IReadOnlyDictionary<string, string[]> pathToLines, string optionName)
    {
        return EffectiveConfigParserBehaviorMap.GetBehavior(optionName) switch
        {
            EffectiveConfigParserBehavior.Flag => DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, optionName),
            EffectiveConfigParserBehavior.LastWins => DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, optionName),
            EffectiveConfigParserBehavior.Multi => DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, optionName),
            _ => DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, optionName),
        };
    }

    /// <summary>Dispatches to GetFlag/GetLastValue/GetMultiValue WithSource based on <see cref="EffectiveConfigParserBehaviorMap"/>.</summary>
    private static object ParseOptionWithSource(IReadOnlyList<string> paths, IReadOnlyDictionary<string, string[]> pathToLines, string optionName, string? managedFilePath)
    {
        return EffectiveConfigParserBehaviorMap.GetBehavior(optionName) switch
        {
            EffectiveConfigParserBehavior.Flag => DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, optionName, managedFilePath),
            EffectiveConfigParserBehavior.LastWins => DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, optionName, managedFilePath),
            EffectiveConfigParserBehavior.Multi => DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, optionName, managedFilePath),
            _ => DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, optionName, managedFilePath),
        };
    }

    private static EffectiveDnsmasqConfig BuildEffectiveConfig(IReadOnlyList<string> paths, IReadOnlyDictionary<string, string[]> pathToLines)
    {
        var noHosts = DnsmasqConfIncludeParser.GetNoHostsFromConfigFiles(paths, pathToLines);
        var addnHosts = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(paths, pathToLines);
        var (hostsdirVal, hostsdirDir) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Hostsdir);
        var hostsdirPath = string.IsNullOrWhiteSpace(hostsdirVal) ? null : DnsmasqConfIncludeParser.ResolvePath(hostsdirVal?.Trim(), hostsdirDir);
        var serverLocal = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.ServerLocalKeys);
        var revServer = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.RevServer);
        var addressValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Address);
        var interfaces = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Interface);
        var listenAddresses = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.ListenAddress);
        var exceptInterfaces = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.ExceptInterface);
        var dhcpRanges = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpRange);
        var dhcpHostLines = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpHost);
        var dhcpOptionLines = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpOption);
        var dhcpMatchValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpMatch);
        var dhcpBootValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpBoot);
        var dhcpIgnoreValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpIgnore);
        var dhcpVendorclassValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpVendorclass);
        var dhcpUserclassValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpUserclass);
        var raParamValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.RaParam);
        var slaacValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Slaac);
        var pxeServiceValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.PxeService);
        var trustAnchorValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.TrustAnchor);
        var resolvFiles = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.ResolvFile);
        var rebindDomainOk = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.RebindDomainOk);
        var bogusNxdomain = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.BogusNxdomain);
        var ignoreAddress = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.IgnoreAddress);
        var alias = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Alias);
        var filterRr = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.FilterRr);
        var cacheRr = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.CacheRr);
        var authServer = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.AuthServer);
        var noDhcpInterface = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.NoDhcpInterface);
        var noDhcpv4Interface = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.NoDhcpv4Interface);
        var noDhcpv6Interface = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.NoDhcpv6Interface);
        var domainValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Domain);
        var cnameValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Cname);
        var mxHostValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.MxHost);
        var srvValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Srv);
        var ptrRecordValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.PtrRecord);
        var txtRecordValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.TxtRecord);
        var naptrRecordValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.NaptrRecord);
        var hostRecordValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.HostRecord);
        var dynamicHostValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DynamicHost);
        var interfaceNameValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.InterfaceName);
        var dhcpOptionForceLines = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpOptionForce);
        var ipsetValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Ipset);
        var nftsetValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Nftset);
        var dhcpMacValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpMac);
        var dhcpNameMatchValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpNameMatch);
        var dhcpIgnoreNamesValues = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpIgnoreNames);
        var dhcpHostsfilePaths = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpHostsfile);
        var dhcpOptsfilePaths = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpOptsfile);
        var dhcpHostsdirPaths = (IReadOnlyList<string>)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpHostsdir);

        var expandHosts = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.ExpandHosts);
        var bogusPriv = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.BogusPriv);
        var strictOrder = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.StrictOrder);
        var allServers = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.AllServers);
        var noResolv = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.NoResolv);
        var domainNeeded = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DomainNeeded);
        var noPoll = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.NoPoll);
        var bindInterfaces = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.BindInterfaces);
        var bindDynamic = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.BindDynamic);
        var noNegcache = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.NoNegcache);
        var dnsLoopDetect = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DnsLoopDetect);
        var stopDnsRebind = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.StopDnsRebind);
        var rebindLocalhostOk = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.RebindLocalhostOk);
        var clearOnReload = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.ClearOnReload);
        var filterwin2k = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Filterwin2k);
        var filterA = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.FilterA);
        var filterAaaa = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.FilterAaaa);
        var localiseQueries = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.LocaliseQueries);
        var logDebug = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.LogDebug);
        var dhcpAuthoritative = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpAuthoritative);
        var leasefileRo = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.LeasefileRo);
        var enableTftp = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.EnableTftp);
        var tftpSecure = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.TftpSecure);
        var dnssec = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Dnssec);
        var dnssecCheckUnsigned = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DnssecCheckUnsigned);
        var readEthers = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.ReadEthers);
        var dhcpRapidCommit = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpRapidCommit);
        var tftpNoFail = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.TftpNoFail);
        var tftpNoBlocksize = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.TftpNoBlocksize);
        var localmx = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Localmx);
        var selfmx = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Selfmx);
        var enableRa = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.EnableRa);
        var logDhcp = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.LogDhcp);
        var keepInForeground = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.KeepInForeground);
        var noDaemon = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.NoDaemon);
        var proxyDnssec = (bool)ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.ProxyDnssec);

        var dhcpLeaseFilePath = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(paths, pathToLines);

        var (cacheVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.CacheSize);
        var cacheSize = TryParseInt(cacheVal);
        var (portVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Port);
        var port = TryParseInt(portVal);
        var (localTtlVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.LocalTtl);
        var localTtl = TryParseInt(localTtlVal);
        var (pidVal, pidDir) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.PidFile);
        var pidFilePath = DnsmasqConfIncludeParser.ResolvePath(pidVal, pidDir);
        var (userVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.User);
        var user = string.IsNullOrWhiteSpace(userVal) ? null : userVal.Trim();
        var (groupVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Group);
        var group = string.IsNullOrWhiteSpace(groupVal) ? null : groupVal.Trim();
        var (logFacVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.LogFacility);
        var logFacility = string.IsNullOrWhiteSpace(logFacVal) ? null : logFacVal.Trim();
        var (logQueriesVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.LogQueries);
        var logQueries = string.IsNullOrWhiteSpace(logQueriesVal) ? null : logQueriesVal.Trim();
        var (authTtlVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.AuthTtl);
        var authTtl = TryParseInt(authTtlVal);
        var (ednsPacketMaxVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.EdnsPacketMax);
        var ednsPacketMax = TryParseInt(ednsPacketMaxVal);
        var (queryPortVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.QueryPort);
        var queryPort = TryParseInt(queryPortVal);
        var (portLimitVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.PortLimit);
        var portLimit = TryParseInt(portLimitVal);
        var (minPortVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.MinPort);
        var minPort = TryParseInt(minPortVal);
        var (maxPortVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.MaxPort);
        var maxPort = TryParseInt(maxPortVal);
        var (logAsyncVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.LogAsync);
        var logAsync = string.IsNullOrWhiteSpace(logAsyncVal) ? null : logAsyncVal.Trim();
        var (localServiceVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.LocalService);
        var localService = string.IsNullOrWhiteSpace(localServiceVal) ? null : localServiceVal.Trim();
        var (leaseMaxVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpLeaseMax);
        var dhcpLeaseMax = TryParseInt(leaseMaxVal);
        var (negTtlVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.NegTtl);
        var negTtl = TryParseInt(negTtlVal);
        var (maxTtlVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.MaxTtl);
        var maxTtl = TryParseInt(maxTtlVal);
        var (maxCacheTtlVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.MaxCacheTtl);
        var maxCacheTtl = TryParseInt(maxCacheTtlVal);
        var (minCacheTtlVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.MinCacheTtl);
        var minCacheTtl = TryParseInt(minCacheTtlVal);
        var (dhcpTtlVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpTtl);
        var dhcpTtl = TryParseInt(dhcpTtlVal);
        var (tftpRootVal, tftpRootDir) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.TftpRoot);
        var tftpRootPath = string.IsNullOrWhiteSpace(tftpRootVal) ? null : DnsmasqConfIncludeParser.ResolvePath(tftpRootVal?.Trim(), tftpRootDir);
        var (pxePromptVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.PxePrompt);
        var pxePrompt = string.IsNullOrWhiteSpace(pxePromptVal) ? null : pxePromptVal.Trim();
        var (enableDbusVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.EnableDbus);
        var enableDbus = string.IsNullOrWhiteSpace(enableDbusVal) ? null : enableDbusVal.Trim();
        var (enableUbusVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.EnableUbus);
        var enableUbus = string.IsNullOrWhiteSpace(enableUbusVal) ? null : enableUbusVal.Trim();
        var (fastDnsRetryVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.FastDnsRetry);
        var fastDnsRetry = string.IsNullOrWhiteSpace(fastDnsRetryVal) ? null : fastDnsRetryVal.Trim();
        var (dhcpScriptVal, dhcpScriptDir) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.DhcpScript);
        var dhcpScriptPath = string.IsNullOrWhiteSpace(dhcpScriptVal) ? null : DnsmasqConfIncludeParser.ResolvePath(dhcpScriptVal?.Trim(), dhcpScriptDir);
        var (mxTargetVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.MxTarget);
        var mxTarget = string.IsNullOrWhiteSpace(mxTargetVal) ? null : mxTargetVal.Trim();
        var (conntrackVal, _) = ((string?, string?))ParseOptionValue(paths, pathToLines, DnsmasqConfKeys.Conntrack);
        var conntrack = string.IsNullOrWhiteSpace(conntrackVal) ? null : conntrackVal.Trim();

        return new EffectiveDnsmasqConfig(
            noHosts, addnHosts, hostsdirPath,
            serverLocal, revServer, addressValues, interfaces, listenAddresses, exceptInterfaces, dhcpRanges, dhcpHostLines, dhcpOptionLines,
            dhcpMatchValues, dhcpBootValues, dhcpIgnoreValues, dhcpVendorclassValues, dhcpUserclassValues, raParamValues, slaacValues, pxeServiceValues, trustAnchorValues, resolvFiles,
            rebindDomainOk, bogusNxdomain, ignoreAddress, alias, filterRr, cacheRr, authServer, noDhcpInterface, noDhcpv4Interface, noDhcpv6Interface,
            domainValues, cnameValues, mxHostValues, srvValues, ptrRecordValues, txtRecordValues, naptrRecordValues, hostRecordValues, dynamicHostValues, interfaceNameValues,
            dhcpOptionForceLines, ipsetValues, nftsetValues, dhcpMacValues, dhcpNameMatchValues, dhcpIgnoreNamesValues,
            dhcpHostsfilePaths, dhcpOptsfilePaths, dhcpHostsdirPaths,
            expandHosts, bogusPriv, strictOrder, allServers, noResolv, domainNeeded, noPoll, bindInterfaces, bindDynamic, noNegcache,
            dnsLoopDetect, stopDnsRebind, rebindLocalhostOk, clearOnReload, filterwin2k, filterA, filterAaaa, localiseQueries, logDebug, dhcpAuthoritative, leasefileRo,
            enableTftp, tftpSecure, tftpNoFail, tftpNoBlocksize, dnssec, dnssecCheckUnsigned, readEthers, dhcpRapidCommit, localmx, selfmx, enableRa, logDhcp,
            keepInForeground, noDaemon, proxyDnssec,
            dhcpLeaseFilePath, cacheSize, port, localTtl, pidFilePath, user, group, logFacility, logQueries,
            authTtl, ednsPacketMax, queryPort, portLimit, minPort, maxPort, logAsync, localService, dhcpLeaseMax,
            negTtl, maxTtl, maxCacheTtl, minCacheTtl, dhcpTtl, tftpRootPath, pxePrompt, enableDbus, enableUbus, fastDnsRetry,
            dhcpScriptPath, mxTarget, conntrack
        );
    }

    private static EffectiveConfigSources BuildEffectiveConfigSources(IReadOnlyList<string> paths, IReadOnlyDictionary<string, string[]> pathToLines, string? managedFilePath, string? managedHostsFilePath)
    {
        var (_, noHostsSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NoHosts, managedFilePath);
        var addnHostsWithSource = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFilesWithSource(paths, pathToLines, managedFilePath, managedHostsFilePath);
        var (_, hostsdirPathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Hostsdir, managedFilePath);
        var serverLocalWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.ServerLocalKeys, managedFilePath);
        var revServerWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.RevServer, managedFilePath);
        var addressWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Address, managedFilePath);
        var interfacesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Interface, managedFilePath);
        var listenAddressesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.ListenAddress, managedFilePath);
        var exceptInterfacesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.ExceptInterface, managedFilePath);
        var dhcpRangesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpRange, managedFilePath);
        var dhcpHostLinesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpHost, managedFilePath);
        var dhcpOptionLinesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpOption, managedFilePath);
        var dhcpMatchWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpMatch, managedFilePath);
        var dhcpBootWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpBoot, managedFilePath);
        var dhcpIgnoreWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpIgnore, managedFilePath);
        var dhcpVendorclassWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpVendorclass, managedFilePath);
        var dhcpUserclassWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpUserclass, managedFilePath);
        var raParamWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.RaParam, managedFilePath);
        var slaacWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Slaac, managedFilePath);
        var pxeServiceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.PxeService, managedFilePath);
        var trustAnchorWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.TrustAnchor, managedFilePath);
        var resolvFilesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.ResolvFile, managedFilePath);
        var rebindDomainOkWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.RebindDomainOk, managedFilePath);
        var bogusNxdomainWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.BogusNxdomain, managedFilePath);
        var ignoreAddressWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.IgnoreAddress, managedFilePath);
        var aliasWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Alias, managedFilePath);
        var filterRrWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.FilterRr, managedFilePath);
        var cacheRrWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.CacheRr, managedFilePath);
        var authServerWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.AuthServer, managedFilePath);
        var noDhcpInterfaceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NoDhcpInterface, managedFilePath);
        var noDhcpv4InterfaceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NoDhcpv4Interface, managedFilePath);
        var noDhcpv6InterfaceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NoDhcpv6Interface, managedFilePath);
        var domainWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Domain, managedFilePath);
        var cnameWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Cname, managedFilePath);
        var mxHostWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.MxHost, managedFilePath);
        var srvWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Srv, managedFilePath);
        var ptrRecordWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.PtrRecord, managedFilePath);
        var txtRecordWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.TxtRecord, managedFilePath);
        var naptrRecordWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NaptrRecord, managedFilePath);
        var hostRecordWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.HostRecord, managedFilePath);
        var dynamicHostWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DynamicHost, managedFilePath);
        var interfaceNameWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.InterfaceName, managedFilePath);
        var dhcpOptionForceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpOptionForce, managedFilePath);
        var ipsetWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Ipset, managedFilePath);
        var nftsetWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Nftset, managedFilePath);
        var dhcpMacWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpMac, managedFilePath);
        var dhcpNameMatchWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpNameMatch, managedFilePath);
        var dhcpIgnoreNamesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpIgnoreNames, managedFilePath);
        var dhcpHostsfileWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpHostsfile, managedFilePath);
        var dhcpOptsfileWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpOptsfile, managedFilePath);
        var dhcpHostsdirWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpHostsdir, managedFilePath);

        var (_, expandHostsSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.ExpandHosts, managedFilePath);
        var (_, bogusPrivSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.BogusPriv, managedFilePath);
        var (_, strictOrderSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.StrictOrder, managedFilePath);
        var (_, allServersSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.AllServers, managedFilePath);
        var (_, noResolvSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NoResolv, managedFilePath);
        var (_, domainNeededSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DomainNeeded, managedFilePath);
        var (_, noPollSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NoPoll, managedFilePath);
        var (_, bindInterfacesSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.BindInterfaces, managedFilePath);
        var (_, bindDynamicSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.BindDynamic, managedFilePath);
        var (_, noNegcacheSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NoNegcache, managedFilePath);
        var (_, dnsLoopDetectSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DnsLoopDetect, managedFilePath);
        var (_, stopDnsRebindSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.StopDnsRebind, managedFilePath);
        var (_, rebindLocalhostOkSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.RebindLocalhostOk, managedFilePath);
        var (_, clearOnReloadSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.ClearOnReload, managedFilePath);
        var (_, filterwin2kSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Filterwin2k, managedFilePath);
        var (_, filterASource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.FilterA, managedFilePath);
        var (_, filterAaaaSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.FilterAaaa, managedFilePath);
        var (_, localiseQueriesSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.LocaliseQueries, managedFilePath);
        var (_, logDebugSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.LogDebug, managedFilePath);
        var (_, dhcpAuthoritativeSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpAuthoritative, managedFilePath);
        var (_, leasefileRoSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.LeasefileRo, managedFilePath);
        var (_, enableTftpSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.EnableTftp, managedFilePath);
        var (_, tftpSecureSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.TftpSecure, managedFilePath);
        var (_, dnssecSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Dnssec, managedFilePath);
        var (_, dnssecCheckUnsignedSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DnssecCheckUnsigned, managedFilePath);
        var (_, readEthersSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.ReadEthers, managedFilePath);
        var (_, dhcpRapidCommitSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpRapidCommit, managedFilePath);
        var (_, tftpNoFailSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.TftpNoFail, managedFilePath);
        var (_, tftpNoBlocksizeSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.TftpNoBlocksize, managedFilePath);
        var (_, localmxSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Localmx, managedFilePath);
        var (_, selfmxSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Selfmx, managedFilePath);
        var (_, enableRaSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.EnableRa, managedFilePath);
        var (_, logDhcpSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.LogDhcp, managedFilePath);
        var (_, keepInForegroundSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.KeepInForeground, managedFilePath);
        var (_, noDaemonSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NoDaemon, managedFilePath);
        var (_, proxyDnssecSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.ProxyDnssec, managedFilePath);

        var (_, dhcpLeaseFilePathSource) = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFilesWithSource(paths, pathToLines, managedFilePath);
        var (_, cacheSizeSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.CacheSize, managedFilePath);
        var (_, portSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Port, managedFilePath);
        var (_, localTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.LocalTtl, managedFilePath);
        var (_, pidFilePathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.PidFile, managedFilePath);
        var (_, userSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.User, managedFilePath);
        var (_, groupSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Group, managedFilePath);
        var (_, logFacilitySource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.LogFacility, managedFilePath);
        var (_, logQueriesSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.LogQueries, managedFilePath);
        var (_, authTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.AuthTtl, managedFilePath);
        var (_, ednsPacketMaxSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.EdnsPacketMax, managedFilePath);
        var (_, queryPortSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.QueryPort, managedFilePath);
        var (_, portLimitSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.PortLimit, managedFilePath);
        var (_, minPortSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.MinPort, managedFilePath);
        var (_, maxPortSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.MaxPort, managedFilePath);
        var (_, logAsyncSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.LogAsync, managedFilePath);
        var (_, localServiceSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.LocalService, managedFilePath);
        var (_, dhcpLeaseMaxSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpLeaseMax, managedFilePath);
        var (_, negTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.NegTtl, managedFilePath);
        var (_, maxTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.MaxTtl, managedFilePath);
        var (_, maxCacheTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.MaxCacheTtl, managedFilePath);
        var (_, minCacheTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.MinCacheTtl, managedFilePath);
        var (_, dhcpTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpTtl, managedFilePath);
        var (_, tftpRootPathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.TftpRoot, managedFilePath);
        var (_, pxePromptSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.PxePrompt, managedFilePath);
        var (_, enableDbusSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.EnableDbus, managedFilePath);
        var (_, enableUbusSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.EnableUbus, managedFilePath);
        var (_, fastDnsRetrySource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.FastDnsRetry, managedFilePath);
        var (_, dhcpScriptPathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpScript, managedFilePath);
        var (_, mxTargetSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.MxTarget, managedFilePath);
        var (_, conntrackSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(paths, pathToLines, DnsmasqConfKeys.Conntrack, managedFilePath);

        return new EffectiveConfigSources(
            noHostsSource, addnHostsWithSource.Select(t => new PathWithSource(t.Path, t.Source)).ToList(), hostsdirPathSource,
            serverLocalWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            revServerWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            addressWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            interfacesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            listenAddressesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            exceptInterfacesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpRangesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpHostLinesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpOptionLinesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpMatchWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpBootWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpIgnoreWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpVendorclassWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpUserclassWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            raParamWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            slaacWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            pxeServiceWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            trustAnchorWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            resolvFilesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            rebindDomainOkWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            bogusNxdomainWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            ignoreAddressWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            aliasWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            filterRrWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            cacheRrWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            authServerWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            noDhcpInterfaceWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            noDhcpv4InterfaceWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            noDhcpv6InterfaceWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            domainWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            cnameWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            mxHostWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            srvWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            ptrRecordWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            txtRecordWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            naptrRecordWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            hostRecordWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dynamicHostWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            interfaceNameWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpOptionForceWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            ipsetWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            nftsetWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpMacWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpNameMatchWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpIgnoreNamesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpHostsfileWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpOptsfileWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpHostsdirWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            expandHostsSource,
            bogusPrivSource,
            strictOrderSource,
            allServersSource,
            noResolvSource,
            domainNeededSource,
            noPollSource,
            bindInterfacesSource,
            bindDynamicSource,
            noNegcacheSource,
            dnsLoopDetectSource,
            stopDnsRebindSource,
            rebindLocalhostOkSource,
            clearOnReloadSource,
            filterwin2kSource,
            filterASource,
            filterAaaaSource,
            localiseQueriesSource,
            logDebugSource,
            dhcpAuthoritativeSource,
            leasefileRoSource,
            enableTftpSource,
            tftpSecureSource,
            tftpNoFailSource,
            tftpNoBlocksizeSource,
            dnssecSource,
            dnssecCheckUnsignedSource,
            readEthersSource,
            dhcpRapidCommitSource,
            localmxSource,
            selfmxSource,
            enableRaSource,
            logDhcpSource,
            keepInForegroundSource,
            noDaemonSource,
            proxyDnssecSource,
            dhcpLeaseFilePathSource,
            cacheSizeSource,
            portSource,
            localTtlSource,
            pidFilePathSource,
            userSource,
            groupSource,
            logFacilitySource,
            logQueriesSource,
            authTtlSource,
            ednsPacketMaxSource,
            queryPortSource,
            portLimitSource,
            minPortSource,
            maxPortSource,
            logAsyncSource,
            localServiceSource,
            dhcpLeaseMaxSource,
            negTtlSource,
            maxTtlSource,
            maxCacheTtlSource,
            minCacheTtlSource,
            dhcpTtlSource,
            tftpRootPathSource,
            pxePromptSource,
            enableDbusSource,
            enableUbusSource,
            fastDnsRetrySource,
            dhcpScriptPathSource,
            mxTargetSource,
            conntrackSource
        );
    }

    private static ManagedConfigContent BuildManagedContent(IReadOnlyDictionary<string, string[]> pathToLines, string? managedFilePath)
    {
        if (string.IsNullOrEmpty(managedFilePath))
            return new ManagedConfigContent(Array.Empty<DnsmasqConfLine>(), "");
        var canonical = Path.GetFullPath(managedFilePath);
        if (!pathToLines.TryGetValue(canonical, out var lines))
            return new ManagedConfigContent(Array.Empty<DnsmasqConfLine>(), "");
        var configLines = DnsmasqConfFileLineParser.ParseFile(lines);
        var effectiveHostsPath = configLines.OfType<AddnHostsLine>().FirstOrDefault()?.AddnHostsPath ?? "";
        return new ManagedConfigContent(configLines, effectiveHostsPath);
    }

    private static List<DhcpHostEntry> BuildDhcpHostEntries(DnsmasqConfigSet set, IReadOnlyDictionary<string, string[]> pathToLines)
    {
        var result = new List<DhcpHostEntry>();
        foreach (var file in set.Files)
        {
            var canonical = Path.GetFullPath(file.Path);
            if (!pathToLines.TryGetValue(canonical, out var lines))
                continue;
            var configLines = DnsmasqConfFileLineParser.ParseFile(lines);
            foreach (var dhcpLine in configLines.OfType<DhcpHostLine>())
            {
                var entry = dhcpLine.DhcpHost;
                entry.SourcePath = file.Path;
                entry.IsEditable = file.IsManaged;
                result.Add(entry);
            }
        }
        return result;
    }

    private static EffectiveDnsmasqConfig CreateDefaultEffectiveConfig() =>
        new(
            NoHosts: false, AddnHostsPaths: Array.Empty<string>(), HostsdirPath: null,
            ServerLocalValues: Array.Empty<string>(), RevServerValues: Array.Empty<string>(), AddressValues: Array.Empty<string>(), Interfaces: Array.Empty<string>(),
            ListenAddresses: Array.Empty<string>(), ExceptInterfaces: Array.Empty<string>(), DhcpRanges: Array.Empty<string>(),
            DhcpHostLines: Array.Empty<string>(), DhcpOptionLines: Array.Empty<string>(),
            DhcpMatchValues: Array.Empty<string>(), DhcpBootValues: Array.Empty<string>(), DhcpIgnoreValues: Array.Empty<string>(),
            DhcpVendorclassValues: Array.Empty<string>(), DhcpUserclassValues: Array.Empty<string>(),
            RaParamValues: Array.Empty<string>(), SlaacValues: Array.Empty<string>(), PxeServiceValues: Array.Empty<string>(), TrustAnchorValues: Array.Empty<string>(),
            ResolvFiles: Array.Empty<string>(), RebindDomainOkValues: Array.Empty<string>(), BogusNxdomainValues: Array.Empty<string>(), IgnoreAddressValues: Array.Empty<string>(),
            AliasValues: Array.Empty<string>(), FilterRrValues: Array.Empty<string>(), CacheRrValues: Array.Empty<string>(),
            AuthServerValues: Array.Empty<string>(), NoDhcpInterfaceValues: Array.Empty<string>(), NoDhcpv4InterfaceValues: Array.Empty<string>(),             NoDhcpv6InterfaceValues: Array.Empty<string>(),
            DomainValues: Array.Empty<string>(), CnameValues: Array.Empty<string>(), MxHostValues: Array.Empty<string>(), SrvValues: Array.Empty<string>(),
            PtrRecordValues: Array.Empty<string>(), TxtRecordValues: Array.Empty<string>(), NaptrRecordValues: Array.Empty<string>(),
            HostRecordValues: Array.Empty<string>(), DynamicHostValues: Array.Empty<string>(), InterfaceNameValues: Array.Empty<string>(),
            DhcpOptionForceLines: Array.Empty<string>(), IpsetValues: Array.Empty<string>(), NftsetValues: Array.Empty<string>(),
            DhcpMacValues: Array.Empty<string>(), DhcpNameMatchValues: Array.Empty<string>(), DhcpIgnoreNamesValues: Array.Empty<string>(),
            DhcpHostsfilePaths: Array.Empty<string>(), DhcpOptsfilePaths: Array.Empty<string>(), DhcpHostsdirPaths: Array.Empty<string>(),
            ExpandHosts: false, BogusPriv: false, StrictOrder: false, AllServers: false, NoResolv: false, DomainNeeded: false, NoPoll: false,
            BindInterfaces: false, BindDynamic: false, NoNegcache: false, DnsLoopDetect: false, StopDnsRebind: false, RebindLocalhostOk: false, ClearOnReload: false,
            Filterwin2k: false, FilterA: false, FilterAaaa: false, LocaliseQueries: false, LogDebug: false, DhcpAuthoritative: false, LeasefileRo: false,
            EnableTftp: false, TftpSecure: false, TftpNoFail: false, TftpNoBlocksize: false, Dnssec: false, DnssecCheckUnsigned: false,
            ReadEthers: false, DhcpRapidCommit: false, Localmx: false, Selfmx: false, EnableRa: false, LogDhcp: false,
            KeepInForeground: false, NoDaemon: false, ProxyDnssec: false,
            DhcpLeaseFilePath: null, CacheSize: null, Port: null, LocalTtl: null, PidFilePath: null, User: null, Group: null,
            LogFacility: null, LogQueries: null, AuthTtl: null, EdnsPacketMax: null, QueryPort: null, PortLimit: null, MinPort: null, MaxPort: null, LogAsync: null, LocalService: null,
            DhcpLeaseMax: null, NegTtl: null, MaxTtl: null, MaxCacheTtl: null, MinCacheTtl: null, DhcpTtl: null,
            TftpRootPath: null, PxePrompt: null, EnableDbus: null, EnableUbus: null, FastDnsRetry: null,
            DhcpScriptPath: null, MxTarget: null, Conntrack: null
        );

    private static EffectiveConfigSources CreateDefaultEffectiveConfigSources() =>
        new(
            NoHosts: null, AddnHostsPaths: Array.Empty<PathWithSource>(), HostsdirPath: null,
            ServerLocalValues: Array.Empty<ValueWithSource>(), RevServerValues: Array.Empty<ValueWithSource>(), AddressValues: Array.Empty<ValueWithSource>(),
            Interfaces: Array.Empty<ValueWithSource>(), ListenAddresses: Array.Empty<ValueWithSource>(),
            ExceptInterfaces: Array.Empty<ValueWithSource>(), DhcpRanges: Array.Empty<ValueWithSource>(),
            DhcpHostLines: Array.Empty<ValueWithSource>(),
            DhcpOptionLines: Array.Empty<ValueWithSource>(),
            DhcpMatchValues: Array.Empty<ValueWithSource>(), DhcpBootValues: Array.Empty<ValueWithSource>(), DhcpIgnoreValues: Array.Empty<ValueWithSource>(),
            DhcpVendorclassValues: Array.Empty<ValueWithSource>(), DhcpUserclassValues: Array.Empty<ValueWithSource>(),
            RaParamValues: Array.Empty<ValueWithSource>(), SlaacValues: Array.Empty<ValueWithSource>(), PxeServiceValues: Array.Empty<ValueWithSource>(), TrustAnchorValues: Array.Empty<ValueWithSource>(),
            ResolvFiles: Array.Empty<ValueWithSource>(),
            RebindDomainOkValues: Array.Empty<ValueWithSource>(), BogusNxdomainValues: Array.Empty<ValueWithSource>(), IgnoreAddressValues: Array.Empty<ValueWithSource>(),
            AliasValues: Array.Empty<ValueWithSource>(), FilterRrValues: Array.Empty<ValueWithSource>(), CacheRrValues: Array.Empty<ValueWithSource>(),
            AuthServerValues: Array.Empty<ValueWithSource>(), NoDhcpInterfaceValues: Array.Empty<ValueWithSource>(), NoDhcpv4InterfaceValues: Array.Empty<ValueWithSource>(),
            NoDhcpv6InterfaceValues: Array.Empty<ValueWithSource>(),
            DomainValues: Array.Empty<ValueWithSource>(), CnameValues: Array.Empty<ValueWithSource>(), MxHostValues: Array.Empty<ValueWithSource>(), SrvValues: Array.Empty<ValueWithSource>(),
            PtrRecordValues: Array.Empty<ValueWithSource>(), TxtRecordValues: Array.Empty<ValueWithSource>(), NaptrRecordValues: Array.Empty<ValueWithSource>(),
            HostRecordValues: Array.Empty<ValueWithSource>(), DynamicHostValues: Array.Empty<ValueWithSource>(), InterfaceNameValues: Array.Empty<ValueWithSource>(),
            DhcpOptionForceLines: Array.Empty<ValueWithSource>(), IpsetValues: Array.Empty<ValueWithSource>(), NftsetValues: Array.Empty<ValueWithSource>(),
            DhcpMacValues: Array.Empty<ValueWithSource>(), DhcpNameMatchValues: Array.Empty<ValueWithSource>(), DhcpIgnoreNamesValues: Array.Empty<ValueWithSource>(),
            DhcpHostsfilePaths: Array.Empty<ValueWithSource>(), DhcpOptsfilePaths: Array.Empty<ValueWithSource>(), DhcpHostsdirPaths: Array.Empty<ValueWithSource>(),
            ExpandHosts: null, BogusPriv: null, StrictOrder: null, AllServers: null, NoResolv: null, DomainNeeded: null, NoPoll: null,
            BindInterfaces: null, BindDynamic: null, NoNegcache: null, DnsLoopDetect: null, StopDnsRebind: null, RebindLocalhostOk: null, ClearOnReload: null,
            Filterwin2k: null, FilterA: null, FilterAaaa: null, LocaliseQueries: null, LogDebug: null, DhcpAuthoritative: null, LeasefileRo: null,
            EnableTftp: null, TftpSecure: null, TftpNoFail: null, TftpNoBlocksize: null, Dnssec: null, DnssecCheckUnsigned: null,
            ReadEthers: null, DhcpRapidCommit: null, Localmx: null, Selfmx: null, EnableRa: null, LogDhcp: null,
            KeepInForeground: null, NoDaemon: null, ProxyDnssec: null,
            DhcpLeaseFilePath: null, CacheSize: null, Port: null, LocalTtl: null, PidFilePath: null, User: null, Group: null,
            LogFacility: null, LogQueries: null, AuthTtl: null, EdnsPacketMax: null, QueryPort: null, PortLimit: null, MinPort: null, MaxPort: null, LogAsync: null, LocalService: null,
            DhcpLeaseMax: null, NegTtl: null, MaxTtl: null, MaxCacheTtl: null, MinCacheTtl: null, DhcpTtl: null,
            TftpRootPath: null, PxePrompt: null, EnableDbus: null, EnableUbus: null, FastDnsRetry: null,
            DhcpScriptPath: null, MxTarget: null, Conntrack: null
        );

    private static int? TryParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return int.TryParse(value.Trim(), out var n) ? n : null;
    }

    public void Dispose()
    {
        _watcherMain?.Dispose();
        _watcherMain = null;
        _watcherManaged?.Dispose();
        _watcherManaged = null;
    }
}
