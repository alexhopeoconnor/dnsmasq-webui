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
            var sources = BuildEffectiveConfigSources(paths, pathToLines, set.ManagedFilePath);
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

    private static EffectiveDnsmasqConfig BuildEffectiveConfig(IReadOnlyList<string> paths, IReadOnlyDictionary<string, string[]> pathToLines)
    {
        var noHosts = DnsmasqConfIncludeParser.GetNoHostsFromConfigFiles(paths, pathToLines);
        var addnHosts = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(paths, pathToLines);
        var serverLocal = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.ServerLocalKeys);
        var revServer = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.RevServer);
        var addressValues = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.Address);
        var interfaces = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.Interface);
        var listenAddresses = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.ListenAddress);
        var exceptInterfaces = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.ExceptInterface);
        var dhcpRanges = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.DhcpRange);
        var dhcpHostLines = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.DhcpHost);
        var dhcpOptionLines = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.DhcpOption);
        var resolvFiles = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.ResolvFile);
        var rebindDomainOk = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.RebindDomainOk);
        var bogusNxdomain = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.BogusNxdomain);
        var ignoreAddress = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.IgnoreAddress);
        var alias = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.Alias);
        var filterRr = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.FilterRr);
        var cacheRr = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.CacheRr);
        var authServer = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.AuthServer);
        var noDhcpInterface = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.NoDhcpInterface);
        var noDhcpv4Interface = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.NoDhcpv4Interface);
        var noDhcpv6Interface = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.NoDhcpv6Interface);

        var expandHosts = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.ExpandHosts);
        var bogusPriv = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.BogusPriv);
        var strictOrder = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.StrictOrder);
        var allServers = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.AllServers);
        var noResolv = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.NoResolv);
        var domainNeeded = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.DomainNeeded);
        var noPoll = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.NoPoll);
        var bindInterfaces = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.BindInterfaces);
        var bindDynamic = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.BindDynamic);
        var noNegcache = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.NoNegcache);
        var dnsLoopDetect = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.DnsLoopDetect);
        var stopDnsRebind = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.StopDnsRebind);
        var rebindLocalhostOk = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.RebindLocalhostOk);
        var clearOnReload = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.ClearOnReload);
        var filterwin2k = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.Filterwin2k);
        var filterA = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.FilterA);
        var filterAaaa = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.FilterAaaa);
        var localiseQueries = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.LocaliseQueries);
        var logDebug = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.LogDebug);
        var dhcpAuthoritative = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.DhcpAuthoritative);
        var leasefileRo = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.LeasefileRo);

        var dhcpLeaseFilePath = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(paths, pathToLines);

        var (cacheVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.CacheSize);
        var cacheSize = TryParseInt(cacheVal);
        var (portVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.Port);
        var port = TryParseInt(portVal);
        var (localTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.LocalTtl);
        var localTtl = TryParseInt(localTtlVal);
        var (pidVal, pidDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.PidFile);
        var pidFilePath = DnsmasqConfIncludeParser.ResolvePath(pidVal, pidDir);
        var (userVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.User);
        var user = string.IsNullOrWhiteSpace(userVal) ? null : userVal.Trim();
        var (groupVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.Group);
        var group = string.IsNullOrWhiteSpace(groupVal) ? null : groupVal.Trim();
        var (logFacVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.LogFacility);
        var logFacility = string.IsNullOrWhiteSpace(logFacVal) ? null : logFacVal.Trim();
        var (logQueriesVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.LogQueries);
        var logQueries = string.IsNullOrWhiteSpace(logQueriesVal) ? null : logQueriesVal.Trim();
        var (authTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.AuthTtl);
        var authTtl = TryParseInt(authTtlVal);
        var (ednsPacketMaxVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.EdnsPacketMax);
        var ednsPacketMax = TryParseInt(ednsPacketMaxVal);
        var (queryPortVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.QueryPort);
        var queryPort = TryParseInt(queryPortVal);
        var (portLimitVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.PortLimit);
        var portLimit = TryParseInt(portLimitVal);
        var (minPortVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.MinPort);
        var minPort = TryParseInt(minPortVal);
        var (maxPortVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.MaxPort);
        var maxPort = TryParseInt(maxPortVal);
        var (logAsyncVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.LogAsync);
        var logAsync = string.IsNullOrWhiteSpace(logAsyncVal) ? null : logAsyncVal.Trim();
        var (localServiceVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.LocalService);
        var localService = string.IsNullOrWhiteSpace(localServiceVal) ? null : localServiceVal.Trim();
        var (leaseMaxVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.DhcpLeaseMax);
        var dhcpLeaseMax = TryParseInt(leaseMaxVal);
        var (negTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.NegTtl);
        var negTtl = TryParseInt(negTtlVal);
        var (maxTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.MaxTtl);
        var maxTtl = TryParseInt(maxTtlVal);
        var (maxCacheTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.MaxCacheTtl);
        var maxCacheTtl = TryParseInt(maxCacheTtlVal);
        var (minCacheTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.MinCacheTtl);
        var minCacheTtl = TryParseInt(minCacheTtlVal);
        var (dhcpTtlVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.DhcpTtl);
        var dhcpTtl = TryParseInt(dhcpTtlVal);

        return new EffectiveDnsmasqConfig(
            noHosts, addnHosts,
            serverLocal, revServer, addressValues, interfaces, listenAddresses, exceptInterfaces, dhcpRanges, dhcpHostLines, dhcpOptionLines, resolvFiles,
            rebindDomainOk, bogusNxdomain, ignoreAddress, alias, filterRr, cacheRr, authServer, noDhcpInterface, noDhcpv4Interface, noDhcpv6Interface,
            expandHosts, bogusPriv, strictOrder, allServers, noResolv, domainNeeded, noPoll, bindInterfaces, bindDynamic, noNegcache,
            dnsLoopDetect, stopDnsRebind, rebindLocalhostOk, clearOnReload, filterwin2k, filterA, filterAaaa, localiseQueries, logDebug, dhcpAuthoritative, leasefileRo,
            dhcpLeaseFilePath, cacheSize, port, localTtl, pidFilePath, user, group, logFacility, logQueries,
            authTtl, ednsPacketMax, queryPort, portLimit, minPort, maxPort, logAsync, localService, dhcpLeaseMax,
            negTtl, maxTtl, maxCacheTtl, minCacheTtl, dhcpTtl
        );
    }

    private static EffectiveConfigSources BuildEffectiveConfigSources(IReadOnlyList<string> paths, IReadOnlyDictionary<string, string[]> pathToLines, string? managedFilePath)
    {
        var (_, noHostsSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.NoHosts, managedFilePath);
        var addnHostsWithSource = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFilesWithSource(paths, pathToLines, managedFilePath);
        var serverLocalWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.ServerLocalKeys, managedFilePath);
        var revServerWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.RevServer, managedFilePath);
        var addressWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.Address, managedFilePath);
        var interfacesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.Interface, managedFilePath);
        var listenAddressesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.ListenAddress, managedFilePath);
        var exceptInterfacesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.ExceptInterface, managedFilePath);
        var dhcpRangesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpRange, managedFilePath);
        var dhcpHostLinesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpHost, managedFilePath);
        var dhcpOptionLinesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpOption, managedFilePath);
        var resolvFilesWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.ResolvFile, managedFilePath);
        var rebindDomainOkWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.RebindDomainOk, managedFilePath);
        var bogusNxdomainWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.BogusNxdomain, managedFilePath);
        var ignoreAddressWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.IgnoreAddress, managedFilePath);
        var aliasWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.Alias, managedFilePath);
        var filterRrWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.FilterRr, managedFilePath);
        var cacheRrWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.CacheRr, managedFilePath);
        var authServerWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.AuthServer, managedFilePath);
        var noDhcpInterfaceWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.NoDhcpInterface, managedFilePath);
        var noDhcpv4InterfaceWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.NoDhcpv4Interface, managedFilePath);
        var noDhcpv6InterfaceWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.NoDhcpv6Interface, managedFilePath);

        var (_, expandHostsSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.ExpandHosts, managedFilePath);
        var (_, bogusPrivSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.BogusPriv, managedFilePath);
        var (_, strictOrderSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.StrictOrder, managedFilePath);
        var (_, allServersSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.AllServers, managedFilePath);
        var (_, noResolvSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.NoResolv, managedFilePath);
        var (_, domainNeededSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.DomainNeeded, managedFilePath);
        var (_, noPollSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.NoPoll, managedFilePath);
        var (_, bindInterfacesSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.BindInterfaces, managedFilePath);
        var (_, bindDynamicSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.BindDynamic, managedFilePath);
        var (_, noNegcacheSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.NoNegcache, managedFilePath);
        var (_, dnsLoopDetectSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.DnsLoopDetect, managedFilePath);
        var (_, stopDnsRebindSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.StopDnsRebind, managedFilePath);
        var (_, rebindLocalhostOkSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.RebindLocalhostOk, managedFilePath);
        var (_, clearOnReloadSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.ClearOnReload, managedFilePath);
        var (_, filterwin2kSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.Filterwin2k, managedFilePath);
        var (_, filterASource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.FilterA, managedFilePath);
        var (_, filterAaaaSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.FilterAaaa, managedFilePath);
        var (_, localiseQueriesSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.LocaliseQueries, managedFilePath);
        var (_, logDebugSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.LogDebug, managedFilePath);
        var (_, dhcpAuthoritativeSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpAuthoritative, managedFilePath);
        var (_, leasefileRoSource) = DnsmasqConfIncludeParser.GetFlagFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.LeasefileRo, managedFilePath);

        var (_, dhcpLeaseFilePathSource) = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFilesWithSource(paths, pathToLines, managedFilePath);
        var (_, cacheSizeSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.CacheSize, managedFilePath);
        var (_, portSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.Port, managedFilePath);
        var (_, localTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.LocalTtl, managedFilePath);
        var (_, pidFilePathSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.PidFile, managedFilePath);
        var (_, userSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.User, managedFilePath);
        var (_, groupSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.Group, managedFilePath);
        var (_, logFacilitySource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.LogFacility, managedFilePath);
        var (_, logQueriesSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.LogQueries, managedFilePath);
        var (_, authTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.AuthTtl, managedFilePath);
        var (_, ednsPacketMaxSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.EdnsPacketMax, managedFilePath);
        var (_, queryPortSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.QueryPort, managedFilePath);
        var (_, portLimitSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.PortLimit, managedFilePath);
        var (_, minPortSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.MinPort, managedFilePath);
        var (_, maxPortSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.MaxPort, managedFilePath);
        var (_, logAsyncSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.LogAsync, managedFilePath);
        var (_, localServiceSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.LocalService, managedFilePath);
        var (_, dhcpLeaseMaxSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpLeaseMax, managedFilePath);
        var (_, negTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.NegTtl, managedFilePath);
        var (_, maxTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.MaxTtl, managedFilePath);
        var (_, maxCacheTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.MaxCacheTtl, managedFilePath);
        var (_, minCacheTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.MinCacheTtl, managedFilePath);
        var (_, dhcpTtlSource) = DnsmasqConfIncludeParser.GetLastValueFromConfigFilesWithSource(paths, pathToLines, DnsmasqConfKeys.DhcpTtl, managedFilePath);

        return new EffectiveConfigSources(
            noHostsSource, addnHostsWithSource.Select(t => new PathWithSource(t.Path, t.Source)).ToList(),
            serverLocalWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            revServerWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            addressWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            interfacesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            listenAddressesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            exceptInterfacesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpRangesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpHostLinesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpOptionLinesWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
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
            expandHostsSource, bogusPrivSource, strictOrderSource, allServersSource, noResolvSource, domainNeededSource, noPollSource,
            bindInterfacesSource, bindDynamicSource, noNegcacheSource, dnsLoopDetectSource, stopDnsRebindSource, rebindLocalhostOkSource, clearOnReloadSource,
            filterwin2kSource, filterASource, filterAaaaSource, localiseQueriesSource, logDebugSource, dhcpAuthoritativeSource, leasefileRoSource,
            dhcpLeaseFilePathSource, cacheSizeSource, portSource, localTtlSource, pidFilePathSource, userSource, groupSource,
            logFacilitySource, logQueriesSource, authTtlSource, ednsPacketMaxSource, queryPortSource, portLimitSource, minPortSource, maxPortSource, logAsyncSource, localServiceSource,
            dhcpLeaseMaxSource, negTtlSource, maxTtlSource, maxCacheTtlSource, minCacheTtlSource, dhcpTtlSource
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
            NoHosts: false, AddnHostsPaths: Array.Empty<string>(),
            ServerLocalValues: Array.Empty<string>(), RevServerValues: Array.Empty<string>(), AddressValues: Array.Empty<string>(), Interfaces: Array.Empty<string>(),
            ListenAddresses: Array.Empty<string>(), ExceptInterfaces: Array.Empty<string>(), DhcpRanges: Array.Empty<string>(),
            DhcpHostLines: Array.Empty<string>(), DhcpOptionLines: Array.Empty<string>(), ResolvFiles: Array.Empty<string>(),
            RebindDomainOkValues: Array.Empty<string>(), BogusNxdomainValues: Array.Empty<string>(), IgnoreAddressValues: Array.Empty<string>(),
            AliasValues: Array.Empty<string>(), FilterRrValues: Array.Empty<string>(), CacheRrValues: Array.Empty<string>(),
            AuthServerValues: Array.Empty<string>(), NoDhcpInterfaceValues: Array.Empty<string>(), NoDhcpv4InterfaceValues: Array.Empty<string>(), NoDhcpv6InterfaceValues: Array.Empty<string>(),
            ExpandHosts: false, BogusPriv: false, StrictOrder: false, AllServers: false, NoResolv: false, DomainNeeded: false, NoPoll: false,
            BindInterfaces: false, BindDynamic: false, NoNegcache: false, DnsLoopDetect: false, StopDnsRebind: false, RebindLocalhostOk: false, ClearOnReload: false,
            Filterwin2k: false, FilterA: false, FilterAaaa: false, LocaliseQueries: false, LogDebug: false, DhcpAuthoritative: false, LeasefileRo: false,
            DhcpLeaseFilePath: null, CacheSize: null, Port: null, LocalTtl: null, PidFilePath: null, User: null, Group: null,
            LogFacility: null, LogQueries: null, AuthTtl: null, EdnsPacketMax: null, QueryPort: null, PortLimit: null, MinPort: null, MaxPort: null, LogAsync: null, LocalService: null,
            DhcpLeaseMax: null, NegTtl: null, MaxTtl: null, MaxCacheTtl: null, MinCacheTtl: null, DhcpTtl: null
        );

    private static EffectiveConfigSources CreateDefaultEffectiveConfigSources() =>
        new(
            NoHosts: null, AddnHostsPaths: Array.Empty<PathWithSource>(),
            ServerLocalValues: Array.Empty<ValueWithSource>(), RevServerValues: Array.Empty<ValueWithSource>(), AddressValues: Array.Empty<ValueWithSource>(),
            Interfaces: Array.Empty<ValueWithSource>(), ListenAddresses: Array.Empty<ValueWithSource>(),
            ExceptInterfaces: Array.Empty<ValueWithSource>(), DhcpRanges: Array.Empty<ValueWithSource>(),
            DhcpHostLines: Array.Empty<ValueWithSource>(), DhcpOptionLines: Array.Empty<ValueWithSource>(),
            ResolvFiles: Array.Empty<ValueWithSource>(),
            RebindDomainOkValues: Array.Empty<ValueWithSource>(), BogusNxdomainValues: Array.Empty<ValueWithSource>(), IgnoreAddressValues: Array.Empty<ValueWithSource>(),
            AliasValues: Array.Empty<ValueWithSource>(), FilterRrValues: Array.Empty<ValueWithSource>(), CacheRrValues: Array.Empty<ValueWithSource>(),
            AuthServerValues: Array.Empty<ValueWithSource>(), NoDhcpInterfaceValues: Array.Empty<ValueWithSource>(), NoDhcpv4InterfaceValues: Array.Empty<ValueWithSource>(), NoDhcpv6InterfaceValues: Array.Empty<ValueWithSource>(),
            ExpandHosts: null, BogusPriv: null, StrictOrder: null, AllServers: null, NoResolv: null, DomainNeeded: null, NoPoll: null,
            BindInterfaces: null, BindDynamic: null, NoNegcache: null, DnsLoopDetect: null, StopDnsRebind: null, RebindLocalhostOk: null, ClearOnReload: null,
            Filterwin2k: null, FilterA: null, FilterAaaa: null, LocaliseQueries: null, LogDebug: null, DhcpAuthoritative: null, LeasefileRo: null,
            DhcpLeaseFilePath: null, CacheSize: null, Port: null, LocalTtl: null, PidFilePath: null, User: null, Group: null,
            LogFacility: null, LogQueries: null, AuthTtl: null, EdnsPacketMax: null, QueryPort: null, PortLimit: null, MinPort: null, MaxPort: null, LogAsync: null, LocalService: null,
            DhcpLeaseMax: null, NegTtl: null, MaxTtl: null, MaxCacheTtl: null, MinCacheTtl: null, DhcpTtl: null
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
