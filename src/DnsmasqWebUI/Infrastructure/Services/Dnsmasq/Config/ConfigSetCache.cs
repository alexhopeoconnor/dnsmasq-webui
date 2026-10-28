using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;

/// <summary>
/// Singleton cache for the config set (main + conf-dir + managed file). Reads all files once per refresh,
/// builds effective values from an in-place dnsmasq occurrence stream, and invalidates on source-file
/// watchers, source metadata changes, include-topology membership changes, staleness, or an
/// explicit invalidation request.
/// Self-write: NotifyWeWroteManagedConfig marks the cache dirty and ignores the next managed-file watcher event for a short window.
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
    private readonly Dictionary<string, FileSystemWatcher> _includedFileWatchers = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, ConfigFileStamp> _fileStamps = new Dictionary<string, ConfigFileStamp>(StringComparer.Ordinal);
    private IReadOnlyList<ConfDirTopologyStamp> _confDirTopologyStamps = Array.Empty<ConfDirTopologyStamp>();

    private readonly record struct ConfigFileStamp(bool Exists, long Length, long LastWriteUtcTicks);

    /// <summary>
    /// Stores the effective membership of a configured directory rather than its raw directory
    /// timestamp. That means a new matching config invalidates promptly, while ignored backup,
    /// hidden, or suffix-mismatched files do not churn the cache.
    /// </summary>
    private sealed record ConfDirTopologyStamp(
        DnsmasqConfIncludeParser.ConfDirReference Reference,
        bool Exists,
        IReadOnlyList<string> MatchingFiles);

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
        var paths = DnsmasqManagedPathSet.TryFromOptions(_options);
        if (paths == null)
            return (null, null);
        return (paths.MainConfigPath, paths.ManagedFilePath);
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
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
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

    private void OnFileRenamed(object sender, RenamedEventArgs e) => OnFileChanged(sender, e);

    /// <summary>
    /// Adds narrow watchers for resolved and declared include files. Metadata and conf-dir
    /// membership checks still run on reads so correctness does not depend on best-effort
    /// file-system watcher delivery.
    /// </summary>
    private void SyncIncludedFileWatchers(IEnumerable<string> paths)
    {
        var (mainPath, managedPath) = GetPaths();
        var permanentPaths = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(mainPath))
            permanentPaths.Add(Path.GetFullPath(mainPath));
        if (!string.IsNullOrWhiteSpace(managedPath))
            permanentPaths.Add(Path.GetFullPath(managedPath));

        var desired = paths
            .Select(Path.GetFullPath)
            .Where(path => !permanentPaths.Contains(path))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var existingPath in _includedFileWatchers.Keys.Where(path => !desired.Contains(path)).ToList())
        {
            _includedFileWatchers[existingPath].Dispose();
            _includedFileWatchers.Remove(existingPath);
        }

        foreach (var path in desired)
        {
            if (_includedFileWatchers.ContainsKey(path))
                continue;

            FileSystemWatcher? watcher = null;
            TryAddWatcher(Path.GetDirectoryName(path), Path.GetFileName(path), ref watcher, "included config");
            if (watcher != null)
                _includedFileWatchers[path] = watcher;
        }
    }

    private static ConfigFileStamp GetFileStamp(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                return new ConfigFileStamp(false, 0, 0);
            return new ConfigFileStamp(true, info.Length, info.LastWriteTimeUtc.Ticks);
        }
        catch
        {
            // An inaccessible file will be read as an empty source on refresh. Retain a stable
            // stamp here rather than letting metadata inspection break ordinary status reads.
            return new ConfigFileStamp(false, 0, 0);
        }
    }

    private static IReadOnlyDictionary<string, ConfigFileStamp> CaptureFileStamps(IEnumerable<string> paths)
    {
        var result = new Dictionary<string, ConfigFileStamp>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var canonical = Path.GetFullPath(path);
            result[canonical] = GetFileStamp(canonical);
        }
        return result;
    }

    private bool HasSourceFilesChanged()
    {
        foreach (var (path, stamp) in _fileStamps)
        {
            if (GetFileStamp(path) != stamp)
                return true;
        }

        return HasConfDirTopologyChanged();
    }

    private bool HasConfDirTopologyChanged()
    {
        foreach (var previous in _confDirTopologyStamps)
        {
            var current = CaptureConfDirTopologyStamp(previous.Reference);
            if (current.Exists != previous.Exists ||
                !current.MatchingFiles.SequenceEqual(previous.MatchingFiles, StringComparer.Ordinal))
                return true;
        }

        return false;
    }

    private static IReadOnlyList<ConfDirTopologyStamp> CaptureConfDirTopologyStamps(
        IEnumerable<DnsmasqConfIncludeParser.ConfDirReference> references)
    {
        return references
            .Select(CaptureConfDirTopologyStamp)
            .ToList();
    }

    private static ConfDirTopologyStamp CaptureConfDirTopologyStamp(
        DnsmasqConfIncludeParser.ConfDirReference reference)
    {
        var matchingFiles = DnsmasqConfIncludeParser
            .GetConfDirFiles(reference)
            .Select(Path.GetFullPath)
            .ToArray();

        return new ConfDirTopologyStamp(reference, Directory.Exists(reference.DirectoryPath), matchingFiles);
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
        var paths = DnsmasqManagedPathSet.TryFromOptions(_options);
        if (paths == null)
            return CreateDefaultSnapshot();

        lock (_lock)
        {
            if (!_dirty && _snapshot != null && _lastReadUtc.HasValue)
            {
                if (HasSourceFilesChanged())
                    _dirty = true;

                var ageSeconds = (DateTime.UtcNow - _lastReadUtc.Value).TotalSeconds;
                if (!_dirty && ageSeconds < StaleCacheSeconds)
                    return _snapshot;
                _dirty = true;
            }

            var includeTopology = DnsmasqConfIncludeParser.GetIncludeTopology(paths.MainConfigPath);
            // Capture topology before the loaded-file set. If a directory changes between the
            // two reads, the next request will see the topology mismatch and refresh rather
            // than retaining a snapshot which failed to notice a newly matching file.
            var set = BuildConfigSet(paths.MainConfigPath, paths.ManagedFilePath, paths.ManagedHostsFilePath);
            if (set.Files.Count == 0)
            {
                _snapshot = CreateDefaultSnapshot();
                _fileStamps = CaptureFileStamps(new[] { paths.MainConfigPath, paths.ManagedFilePath });
                _confDirTopologyStamps = CaptureConfDirTopologyStamps(includeTopology.ConfDirectories);
                _lastReadUtc = DateTime.UtcNow;
                _dirty = false;
                return _snapshot;
            }

            var configPaths = set.Files.Select(f => f.Path).ToList();
            var pathToLines = ReadAllPaths(configPaths, ct);
            var occurrences = BuildEffectiveOccurrences(set, pathToLines);
            var config = BuildEffectiveConfig(occurrences);
            var sources = BuildEffectiveConfigSources(occurrences, set.ManagedFilePath, set.ManagedHostsFilePath);
            var managedContent = BuildManagedContent(pathToLines, set.ManagedFilePath);
            var dhcpHostEntries = BuildDhcpHostEntries(set, pathToLines);

            _snapshot = new ConfigSetSnapshot(set, config, sources, managedContent, dhcpHostEntries);
            var trackedPaths = configPaths
                .Concat(includeTopology.ConfFilePaths)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            _fileStamps = CaptureFileStamps(trackedPaths);
            _confDirTopologyStamps = CaptureConfDirTopologyStamps(includeTopology.ConfDirectories);
            SyncIncludedFileWatchers(trackedPaths);
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

    /// <summary>
    /// Builds the exact in-place occurrence stream. During the brief pre-startup window before
    /// <see cref="EnsureManagedConfigHostedService"/> appends the managed conf-file directive,
    /// retain the cache's established overlay contract by evaluating the managed file last.
    /// Startup makes that synthetic tail unnecessary in normal operation.
    /// </summary>
    private static IReadOnlyList<DnsmasqConfigLineOccurrence> BuildEffectiveOccurrences(
        DnsmasqConfigSet set,
        IReadOnlyDictionary<string, string[]> pathToLines)
    {
        var result = DnsmasqConfIncludeParser
            .GetConfigLineOccurrencesInDnsmasqOrder(set.MainConfigPath, pathToLines)
            .ToList();

        if (string.IsNullOrWhiteSpace(set.ManagedFilePath))
            return result;

        var managedPath = Path.GetFullPath(set.ManagedFilePath);
        if (result.Any(occurrence => string.Equals(occurrence.FilePath, managedPath, StringComparison.Ordinal)))
            return result;

        if (!pathToLines.TryGetValue(managedPath, out var managedLines))
            return result;

        for (var i = 0; i < managedLines.Length; i++)
        {
            var line = managedLines[i];
            var directive = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
            if (directive == null ||
                string.Equals(directive.Value.key, DnsmasqConfKeys.ConfFile, StringComparison.Ordinal) ||
                string.Equals(directive.Value.key, DnsmasqConfKeys.ConfDir, StringComparison.Ordinal))
                continue;
            result.Add(new DnsmasqConfigLineOccurrence(managedPath, i + 1, line, DnsmasqConfFileSource.ConfFile));
        }

        return result;
    }

    /// <summary>Dispatches to ordered occurrence readers based on <see cref="EffectiveConfigParserBehaviorMap"/>.</summary>
    private static object ParseOptionValue(IReadOnlyList<DnsmasqConfigLineOccurrence> occurrences, string optionName)
    {
        return EffectiveConfigParserBehaviorMap.GetBehavior(optionName) switch
        {
            EffectiveConfigParserBehavior.Flag => DnsmasqConfIncludeParser.GetFlagFromConfigOccurrences(occurrences, optionName),
            EffectiveConfigParserBehavior.LastWins => DnsmasqConfIncludeParser.GetLastValueFromConfigOccurrences(occurrences, optionName),
            EffectiveConfigParserBehavior.Multi => DnsmasqConfIncludeParser.GetMultiValueFromConfigOccurrences(occurrences, optionName),
            _ => DnsmasqConfIncludeParser.GetLastValueFromConfigOccurrences(occurrences, optionName),
        };
    }

    /// <summary>Dispatches to ordered occurrence readers with source based on <see cref="EffectiveConfigParserBehaviorMap"/>.</summary>
    private static object ParseOptionWithSource(IReadOnlyList<DnsmasqConfigLineOccurrence> occurrences, string optionName, string? managedFilePath)
    {
        return EffectiveConfigParserBehaviorMap.GetBehavior(optionName) switch
        {
            EffectiveConfigParserBehavior.Flag => DnsmasqConfIncludeParser.GetFlagFromConfigOccurrencesWithSource(occurrences, optionName, managedFilePath),
            EffectiveConfigParserBehavior.LastWins => DnsmasqConfIncludeParser.GetLastValueFromConfigOccurrencesWithSource(occurrences, optionName, managedFilePath),
            EffectiveConfigParserBehavior.Multi => DnsmasqConfIncludeParser.GetMultiValueFromConfigOccurrencesWithSource(occurrences, optionName, managedFilePath),
            _ => DnsmasqConfIncludeParser.GetLastValueFromConfigOccurrencesWithSource(occurrences, optionName, managedFilePath),
        };
    }

    private static EffectiveDnsmasqConfig BuildEffectiveConfig(IReadOnlyList<DnsmasqConfigLineOccurrence> occurrences)
    {
        var noHosts = DnsmasqConfIncludeParser.GetFlagFromConfigOccurrences(occurrences, DnsmasqConfKeys.NoHosts);
        var addnHosts = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigOccurrences(occurrences);
        var (hostsdirVal, hostsdirDir) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.Hostsdir);
        var hostsdirPath = string.IsNullOrWhiteSpace(hostsdirVal) ? null : DnsmasqConfIncludeParser.ResolvePath(hostsdirVal?.Trim(), hostsdirDir);
        var serverValues = DnsmasqConfIncludeParser.GetMultiValueFromConfigOccurrences(occurrences, DnsmasqConfKeys.Server);
        var localValues = DnsmasqConfIncludeParser.GetMultiValueFromConfigOccurrences(occurrences, DnsmasqConfKeys.Local);
        var revServer = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.RevServer);
        var addressValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Address);
        var interfaces = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Interface);
        var listenAddresses = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.ListenAddress);
        var exceptInterfaces = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.ExceptInterface);
        var dhcpRanges = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpRange);
        var dhcpHostLines = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpHost);
        var dhcpOptionLines = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpOption);
        var dhcpMatchValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpMatch);
        var dhcpBootValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpBoot);
        var dhcpIgnoreValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpIgnore);
        var dhcpVendorclassValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpVendorclass);
        var dhcpUserclassValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpUserclass);
        var raParamValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.RaParam);
        var slaacValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Slaac);
        var pxeServiceValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.PxeService);
        var trustAnchorValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.TrustAnchor);
        var resolvFiles = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.ResolvFile);
        var rebindDomainOk = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.RebindDomainOk);
        var bogusNxdomain = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.BogusNxdomain);
        var ignoreAddress = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.IgnoreAddress);
        var alias = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Alias);
        var filterRr = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.FilterRr);
        var cacheRr = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.CacheRr);
        var authServer = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.AuthServer);
        var noDhcpInterface = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.NoDhcpInterface);
        var noDhcpv4Interface = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.NoDhcpv4Interface);
        var noDhcpv6Interface = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.NoDhcpv6Interface);
        var domainValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Domain);
        var cnameValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Cname);
        var mxHostValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.MxHost);
        var srvValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Srv);
        var ptrRecordValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.PtrRecord);
        var txtRecordValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.TxtRecord);
        var naptrRecordValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.NaptrRecord);
        var hostRecordValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.HostRecord);
        var dynamicHostValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DynamicHost);
        var interfaceNameValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.InterfaceName);
        var dhcpOptionForceLines = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpOptionForce);
        var ipsetValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Ipset);
        var nftsetValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Nftset);
        var dhcpMacValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpMac);
        var dhcpNameMatchValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpNameMatch);
        var dhcpIgnoreNamesValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpIgnoreNames);
        var dhcpHostsfilePaths = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpHostsfile);
        var dhcpOptsfilePaths = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpOptsfile);
        var dhcpHostsdirPaths = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpHostsdir);
        var dhcpOptsdirPaths = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpOptsdir);
        var connmarkAllowlistValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.ConnmarkAllowlist);
        var caaRecordValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.CaaRecord);
        var dnsRrValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DnsRr);
        var synthDomainValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.SynthDomain);
        var authZoneValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.AuthZone);
        var authSoaValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.AuthSoa);
        var authSecServersValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.AuthSecServers);
        var authPeerValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.AuthPeer);
        var dhcpRelayValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpRelay);
        var dhcpCircuitidValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpCircuitid);
        var dhcpRemoteidValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpRemoteid);
        var dhcpSubscridValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpSubscrid);
        var dhcpProxyValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpProxy);
        var tagIfValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.TagIf);
        var bridgeInterfaceValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.BridgeInterface);
        var sharedNetworkValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.SharedNetwork);
        var dhcpOptionPxeValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpOptionPxe);

        var expandHosts = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.ExpandHosts);
        var bogusPriv = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.BogusPriv);
        var strictOrder = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.StrictOrder);
        var allServers = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.AllServers);
        var noResolv = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.NoResolv);
        var domainNeeded = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.DomainNeeded);
        var noPoll = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.NoPoll);
        var bindInterfaces = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.BindInterfaces);
        var bindDynamic = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.BindDynamic);
        var noNegcache = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.NoNegcache);
        var dnsLoopDetect = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.DnsLoopDetect);
        var stopDnsRebind = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.StopDnsRebind);
        var rebindLocalhostOk = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.RebindLocalhostOk);
        var clearOnReload = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.ClearOnReload);
        var filterwin2k = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.Filterwin2k);
        var filterA = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.FilterA);
        var filterAaaa = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.FilterAaaa);
        var localiseQueries = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.LocaliseQueries);
        var logDebug = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.LogDebug);
        var dhcpAuthoritative = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpAuthoritative);
        var leasefileRo = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.LeasefileRo);
        var enableTftp = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.EnableTftp);
        var tftpSecure = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.TftpSecure);
        var dnssec = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.Dnssec);
        var (dnssecCheckUnsignedVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DnssecCheckUnsigned);
        var dnssecCheckUnsigned = dnssecCheckUnsignedVal == null ? null : (string.IsNullOrWhiteSpace(dnssecCheckUnsignedVal) ? "" : dnssecCheckUnsignedVal.Trim());
        var readEthers = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.ReadEthers);
        var dhcpRapidCommit = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpRapidCommit);
        var tftpNoFail = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.TftpNoFail);
        var tftpNoBlocksize = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.TftpNoBlocksize);
        var localmx = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.Localmx);
        var selfmx = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.Selfmx);
        var enableRa = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.EnableRa);
        var logDhcp = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.LogDhcp);
        var keepInForeground = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.KeepInForeground);
        var noDaemon = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.NoDaemon);
        var proxyDnssec = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.ProxyDnssec);
        var (connmarkAllowlistEnableVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.ConnmarkAllowlistEnable);
        var connmarkAllowlistEnable = connmarkAllowlistEnableVal == null ? null : (string.IsNullOrWhiteSpace(connmarkAllowlistEnableVal) ? "" : connmarkAllowlistEnableVal.Trim());
        var noRoundRobin = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.NoRoundRobin);
        var dnssecNoTimecheck = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.DnssecNoTimecheck);
        var dnssecDebug = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.DnssecDebug);
        var leasequeryValues = (IReadOnlyList<string>)ParseOptionValue(occurrences, DnsmasqConfKeys.Leasequery);
        var (dhcpGenerateNamesVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpGenerateNames);
        var dhcpGenerateNames = dhcpGenerateNamesVal == null ? null : (string.IsNullOrWhiteSpace(dhcpGenerateNamesVal) ? "" : dhcpGenerateNamesVal.Trim());
        var (dhcpBroadcastVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpBroadcast);
        var dhcpBroadcast = dhcpBroadcastVal == null ? null : (string.IsNullOrWhiteSpace(dhcpBroadcastVal) ? "" : dhcpBroadcastVal.Trim());
        var dhcpSequentialIp = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpSequentialIp);
        var dhcpIgnoreClid = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpIgnoreClid);
        var (bootpDynamicVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.BootpDynamic);
        var bootpDynamic = bootpDynamicVal == null ? null : (string.IsNullOrWhiteSpace(bootpDynamicVal) ? "" : bootpDynamicVal.Trim());
        var noPing = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.NoPing);
        var scriptArp = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.ScriptArp);
        var scriptOnRenewal = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.ScriptOnRenewal);
        var dhcpNoOverride = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpNoOverride);
        var quietDhcp = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.QuietDhcp);
        var quietDhcp6 = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.QuietDhcp6);
        var quietRa = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.QuietRa);
        var quietTftp = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.QuietTftp);

        var dhcpLeaseFilePath = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigOccurrences(occurrences);

        var (cacheVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.CacheSize);
        var cacheSize = TryParseInt(cacheVal);
        var (portVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.Port);
        var port = TryParseInt(portVal);
        var (localTtlVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.LocalTtl);
        var localTtl = TryParseInt(localTtlVal);
        var (pidVal, pidDir) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.PidFile);
        var pidFilePath = DnsmasqConfIncludeParser.ResolvePath(pidVal, pidDir);
        var (userVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.User);
        var user = string.IsNullOrWhiteSpace(userVal) ? null : userVal.Trim();
        var (groupVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.Group);
        var group = string.IsNullOrWhiteSpace(groupVal) ? null : groupVal.Trim();
        var (logFacVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.LogFacility);
        var logFacility = string.IsNullOrWhiteSpace(logFacVal) ? null : logFacVal.Trim();
        var (logQueriesVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.LogQueries);
        // log-queries is a key-only-or-value directive: the bare key enables ordinary query
        // logging, while null means the directive is absent. Preserve that distinction so a
        // status refresh and subsequent save cannot turn an enabled setting off.
        var logQueries = logQueriesVal == null ? null : (string.IsNullOrWhiteSpace(logQueriesVal) ? "" : logQueriesVal.Trim());
        var (authTtlVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.AuthTtl);
        var authTtl = TryParseInt(authTtlVal);
        var (ednsPacketMaxVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.EdnsPacketMax);
        var ednsPacketMax = TryParseInt(ednsPacketMaxVal);
        var (queryPortVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.QueryPort);
        var queryPort = TryParseInt(queryPortVal);
        var (portLimitVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.PortLimit);
        var portLimit = TryParseInt(portLimitVal);
        var (minPortVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.MinPort);
        var minPort = TryParseInt(minPortVal);
        var (maxPortVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.MaxPort);
        var maxPort = TryParseInt(maxPortVal);
        var (logAsyncVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.LogAsync);
        var logAsync = string.IsNullOrWhiteSpace(logAsyncVal) ? null : logAsyncVal.Trim();
        var (localServiceVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.LocalService);
        var localService = string.IsNullOrWhiteSpace(localServiceVal) ? null : localServiceVal.Trim();
        var (leaseMaxVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpLeaseMax);
        var dhcpLeaseMax = TryParseInt(leaseMaxVal);
        var (negTtlVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.NegTtl);
        var negTtl = TryParseInt(negTtlVal);
        var (maxTtlVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.MaxTtl);
        var maxTtl = TryParseInt(maxTtlVal);
        var (maxCacheTtlVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.MaxCacheTtl);
        var maxCacheTtl = TryParseInt(maxCacheTtlVal);
        var (minCacheTtlVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.MinCacheTtl);
        var minCacheTtl = TryParseInt(minCacheTtlVal);
        var (dhcpTtlVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpTtl);
        var dhcpTtl = TryParseInt(dhcpTtlVal);
        var (tftpRootVal, tftpRootDir) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.TftpRoot);
        var tftpRootPath = string.IsNullOrWhiteSpace(tftpRootVal) ? null : DnsmasqConfIncludeParser.ResolvePath(tftpRootVal?.Trim(), tftpRootDir);
        var (pxePromptVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.PxePrompt);
        var pxePrompt = string.IsNullOrWhiteSpace(pxePromptVal) ? null : pxePromptVal.Trim();
        var (enableDbusVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.EnableDbus);
        var enableDbus = string.IsNullOrWhiteSpace(enableDbusVal) ? null : enableDbusVal.Trim();
        var (enableUbusVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.EnableUbus);
        var enableUbus = string.IsNullOrWhiteSpace(enableUbusVal) ? null : enableUbusVal.Trim();
        var (fastDnsRetryVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.FastDnsRetry);
        var fastDnsRetry = string.IsNullOrWhiteSpace(fastDnsRetryVal) ? null : fastDnsRetryVal.Trim();
        var (dhcpScriptVal, dhcpScriptDir) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpScript);
        var dhcpScriptPath = string.IsNullOrWhiteSpace(dhcpScriptVal) ? null : DnsmasqConfIncludeParser.ResolvePath(dhcpScriptVal?.Trim(), dhcpScriptDir);
        var (mxTargetVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.MxTarget);
        var mxTarget = string.IsNullOrWhiteSpace(mxTargetVal) ? null : mxTargetVal.Trim();
        var (dnsForwardMaxVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DnsForwardMax);
        var dnsForwardMax = TryParseInt(dnsForwardMaxVal);
        var (dumpfileVal, dumpfileDir) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.Dumpfile);
        var dumpfilePath = string.IsNullOrWhiteSpace(dumpfileVal) ? null : DnsmasqConfIncludeParser.ResolvePath(dumpfileVal.Trim(), dumpfileDir);
        var (dumpmaskVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.Dumpmask);
        var dumpmask = string.IsNullOrWhiteSpace(dumpmaskVal) ? null : dumpmaskVal.Trim();
        var (addCpeIdVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.AddCpeId);
        var addCpeId = string.IsNullOrWhiteSpace(addCpeIdVal) ? null : addCpeIdVal.Trim();
        var (dnssecTimestampVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DnssecTimestamp);
        var dnssecTimestamp = string.IsNullOrWhiteSpace(dnssecTimestampVal) ? null : dnssecTimestampVal.Trim();
        var (dnssecLimitsVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DnssecLimits);
        var dnssecLimits = string.IsNullOrWhiteSpace(dnssecLimitsVal) ? null : dnssecLimitsVal.Trim();
        var (dhcpAlternatePortVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpAlternatePort);
        var dhcpAlternatePort = string.IsNullOrWhiteSpace(dhcpAlternatePortVal) ? null : dhcpAlternatePortVal.Trim();
        var (dhcpDuidVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpDuid);
        var dhcpDuid = string.IsNullOrWhiteSpace(dhcpDuidVal) ? null : dhcpDuidVal.Trim();
        var (dhcpLuascriptVal, dhcpLuascriptDir) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpLuascript);
        var dhcpLuascriptPath = string.IsNullOrWhiteSpace(dhcpLuascriptVal) ? null : DnsmasqConfIncludeParser.ResolvePath(dhcpLuascriptVal?.Trim(), dhcpLuascriptDir);
        var (dhcpScriptuserVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpScriptuser);
        var dhcpScriptuser = string.IsNullOrWhiteSpace(dhcpScriptuserVal) ? null : dhcpScriptuserVal.Trim();
        var (dhcpPxeVendorVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.DhcpPxeVendor);
        var dhcpPxeVendor = string.IsNullOrWhiteSpace(dhcpPxeVendorVal) ? null : dhcpPxeVendorVal.Trim();
        var (useStaleCacheVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.UseStaleCache);
        var useStaleCache = useStaleCacheVal == null ? null : (string.IsNullOrWhiteSpace(useStaleCacheVal) ? "" : useStaleCacheVal.Trim());
        var (addMacVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.AddMac);
        var addMac = addMacVal == null ? null : (string.IsNullOrWhiteSpace(addMacVal) ? "" : addMacVal.Trim());
        var stripMac = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.StripMac);
        var (addSubnetVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.AddSubnet);
        var addSubnet = addSubnetVal == null ? null : (string.IsNullOrWhiteSpace(addSubnetVal) ? "" : addSubnetVal.Trim());
        var stripSubnet = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.StripSubnet);
        var (umbrellaVal, _) = ((string?, string?))ParseOptionValue(occurrences, DnsmasqConfKeys.Umbrella);
        var umbrella = umbrellaVal == null ? null : (string.IsNullOrWhiteSpace(umbrellaVal) ? "" : umbrellaVal.Trim());
        var do0x20 = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.Do0x20Encode);
        var no0x20 = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.No0x20Encode);
        // dnsmasq docs: no-0x20-encode overrides do-0x20-encode
        var do0x20EncodeState = no0x20 ? ExplicitToggleState.Disabled : (do0x20 ? ExplicitToggleState.Enabled : ExplicitToggleState.Default);
        var conntrack = (bool)ParseOptionValue(occurrences, DnsmasqConfKeys.Conntrack);

        return new EffectiveDnsmasqConfig(
            noHosts, addnHosts, hostsdirPath,
            serverValues, localValues, revServer, addressValues, interfaces, listenAddresses, exceptInterfaces, dhcpRanges, dhcpHostLines, dhcpOptionLines,
            dhcpMatchValues, dhcpBootValues, dhcpIgnoreValues, dhcpVendorclassValues, dhcpUserclassValues, raParamValues, slaacValues, pxeServiceValues, trustAnchorValues, resolvFiles,
            rebindDomainOk, bogusNxdomain, ignoreAddress, alias, filterRr, cacheRr, authServer, noDhcpInterface, noDhcpv4Interface, noDhcpv6Interface,
            domainValues, cnameValues, mxHostValues, srvValues, ptrRecordValues, txtRecordValues, naptrRecordValues, hostRecordValues, dynamicHostValues, interfaceNameValues,
            dhcpOptionForceLines, ipsetValues, nftsetValues, dhcpMacValues, dhcpNameMatchValues, dhcpIgnoreNamesValues,
            dhcpHostsfilePaths, dhcpOptsfilePaths, dhcpHostsdirPaths, dhcpOptsdirPaths,
            connmarkAllowlistValues, caaRecordValues, dnsRrValues, synthDomainValues, authZoneValues, authSoaValues, authSecServersValues, authPeerValues,
            dhcpRelayValues, dhcpCircuitidValues, dhcpRemoteidValues, dhcpSubscridValues, dhcpProxyValues, tagIfValues, bridgeInterfaceValues, sharedNetworkValues, dhcpOptionPxeValues,
            expandHosts, bogusPriv, strictOrder, allServers, noResolv, domainNeeded, noPoll, bindInterfaces, bindDynamic, noNegcache,
            dnsLoopDetect, stopDnsRebind, rebindLocalhostOk, clearOnReload, filterwin2k, filterA, filterAaaa, localiseQueries, logDebug, dhcpAuthoritative, leasefileRo,
            enableTftp, tftpSecure, tftpNoFail, tftpNoBlocksize, dnssec, dnssecCheckUnsigned, readEthers, dhcpRapidCommit, localmx, selfmx, enableRa, logDhcp,
            keepInForeground, noDaemon, proxyDnssec,
            connmarkAllowlistEnable, noRoundRobin, dnssecNoTimecheck, dnssecDebug, leasequeryValues, dhcpGenerateNames, dhcpBroadcast, dhcpSequentialIp, dhcpIgnoreClid, bootpDynamic, noPing, scriptArp, scriptOnRenewal, dhcpNoOverride, quietDhcp, quietDhcp6, quietRa, quietTftp,
            dhcpLeaseFilePath, cacheSize, port, localTtl, pidFilePath, user, group, logFacility, logQueries,
            authTtl, ednsPacketMax, queryPort, portLimit, minPort, maxPort, logAsync, localService, dhcpLeaseMax,
            negTtl, maxTtl, maxCacheTtl, minCacheTtl, dhcpTtl, tftpRootPath, pxePrompt, enableDbus, enableUbus, fastDnsRetry,
            dhcpScriptPath, mxTarget, dnsForwardMax, dumpfilePath, dumpmask, addCpeId, dnssecTimestamp, dnssecLimits, dhcpAlternatePort, dhcpDuid, dhcpLuascriptPath, dhcpScriptuser, dhcpPxeVendor,
            useStaleCache, addMac, stripMac, addSubnet, stripSubnet, umbrella, do0x20EncodeState, conntrack
        );
    }

    private static EffectiveConfigSources BuildEffectiveConfigSources(IReadOnlyList<DnsmasqConfigLineOccurrence> occurrences, string? managedFilePath, string? managedHostsFilePath)
    {
        var (_, noHostsSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoHosts, managedFilePath);
        var addnHostsWithSource = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigOccurrencesWithSource(occurrences, managedFilePath, managedHostsFilePath);
        var (_, hostsdirPathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Hostsdir, managedFilePath);
        var serverWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigOccurrencesWithSource(occurrences, DnsmasqConfKeys.Server, managedFilePath);
        var localWithSource = DnsmasqConfIncludeParser.GetMultiValueFromConfigOccurrencesWithSource(occurrences, DnsmasqConfKeys.Local, managedFilePath);
        var revServerWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.RevServer, managedFilePath);
        var addressWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Address, managedFilePath);
        var interfacesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Interface, managedFilePath);
        var listenAddressesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.ListenAddress, managedFilePath);
        var exceptInterfacesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.ExceptInterface, managedFilePath);
        var dhcpRangesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpRange, managedFilePath);
        var dhcpHostLinesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpHost, managedFilePath);
        var dhcpOptionLinesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpOption, managedFilePath);
        var dhcpMatchWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpMatch, managedFilePath);
        var dhcpBootWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpBoot, managedFilePath);
        var dhcpIgnoreWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpIgnore, managedFilePath);
        var dhcpVendorclassWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpVendorclass, managedFilePath);
        var dhcpUserclassWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpUserclass, managedFilePath);
        var raParamWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.RaParam, managedFilePath);
        var slaacWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Slaac, managedFilePath);
        var pxeServiceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.PxeService, managedFilePath);
        var trustAnchorWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.TrustAnchor, managedFilePath);
        var resolvFilesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.ResolvFile, managedFilePath);
        var rebindDomainOkWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.RebindDomainOk, managedFilePath);
        var bogusNxdomainWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.BogusNxdomain, managedFilePath);
        var ignoreAddressWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.IgnoreAddress, managedFilePath);
        var aliasWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Alias, managedFilePath);
        var filterRrWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.FilterRr, managedFilePath);
        var cacheRrWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.CacheRr, managedFilePath);
        var authServerWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.AuthServer, managedFilePath);
        var noDhcpInterfaceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoDhcpInterface, managedFilePath);
        var noDhcpv4InterfaceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoDhcpv4Interface, managedFilePath);
        var noDhcpv6InterfaceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoDhcpv6Interface, managedFilePath);
        var domainWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Domain, managedFilePath);
        var cnameWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Cname, managedFilePath);
        var mxHostWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.MxHost, managedFilePath);
        var srvWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Srv, managedFilePath);
        var ptrRecordWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.PtrRecord, managedFilePath);
        var txtRecordWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.TxtRecord, managedFilePath);
        var naptrRecordWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.NaptrRecord, managedFilePath);
        var hostRecordWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.HostRecord, managedFilePath);
        var dynamicHostWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DynamicHost, managedFilePath);
        var interfaceNameWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.InterfaceName, managedFilePath);
        var dhcpOptionForceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpOptionForce, managedFilePath);
        var ipsetWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Ipset, managedFilePath);
        var nftsetWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Nftset, managedFilePath);
        var dhcpMacWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpMac, managedFilePath);
        var dhcpNameMatchWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpNameMatch, managedFilePath);
        var dhcpIgnoreNamesWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpIgnoreNames, managedFilePath);
        var dhcpHostsfileWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpHostsfile, managedFilePath);
        var dhcpOptsfileWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpOptsfile, managedFilePath);
        var dhcpHostsdirWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpHostsdir, managedFilePath);
        var dhcpOptsdirWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpOptsdir, managedFilePath);
        var connmarkAllowlistWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.ConnmarkAllowlist, managedFilePath);
        var caaRecordWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.CaaRecord, managedFilePath);
        var dnsRrWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DnsRr, managedFilePath);
        var synthDomainWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.SynthDomain, managedFilePath);
        var authZoneWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.AuthZone, managedFilePath);
        var authSoaWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.AuthSoa, managedFilePath);
        var authSecServersWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.AuthSecServers, managedFilePath);
        var authPeerWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.AuthPeer, managedFilePath);
        var dhcpRelayWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpRelay, managedFilePath);
        var dhcpCircuitidWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpCircuitid, managedFilePath);
        var dhcpRemoteidWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpRemoteid, managedFilePath);
        var dhcpSubscridWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpSubscrid, managedFilePath);
        var dhcpProxyWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpProxy, managedFilePath);
        var tagIfWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.TagIf, managedFilePath);
        var bridgeInterfaceWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.BridgeInterface, managedFilePath);
        var sharedNetworkWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.SharedNetwork, managedFilePath);
        var dhcpOptionPxeWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpOptionPxe, managedFilePath);

        var (_, expandHostsSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.ExpandHosts, managedFilePath);
        var (_, bogusPrivSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.BogusPriv, managedFilePath);
        var (_, strictOrderSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.StrictOrder, managedFilePath);
        var (_, allServersSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.AllServers, managedFilePath);
        var (_, noResolvSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoResolv, managedFilePath);
        var (_, domainNeededSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DomainNeeded, managedFilePath);
        var (_, noPollSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoPoll, managedFilePath);
        var (_, bindInterfacesSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.BindInterfaces, managedFilePath);
        var (_, bindDynamicSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.BindDynamic, managedFilePath);
        var (_, noNegcacheSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoNegcache, managedFilePath);
        var (_, dnsLoopDetectSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DnsLoopDetect, managedFilePath);
        var (_, stopDnsRebindSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.StopDnsRebind, managedFilePath);
        var (_, rebindLocalhostOkSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.RebindLocalhostOk, managedFilePath);
        var (_, clearOnReloadSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.ClearOnReload, managedFilePath);
        var (_, filterwin2kSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Filterwin2k, managedFilePath);
        var (_, filterASource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.FilterA, managedFilePath);
        var (_, filterAaaaSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.FilterAaaa, managedFilePath);
        var (_, localiseQueriesSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.LocaliseQueries, managedFilePath);
        var (_, logDebugSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.LogDebug, managedFilePath);
        var (_, dhcpAuthoritativeSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpAuthoritative, managedFilePath);
        var (_, leasefileRoSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.LeasefileRo, managedFilePath);
        var (_, enableTftpSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.EnableTftp, managedFilePath);
        var (_, tftpSecureSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.TftpSecure, managedFilePath);
        var (_, dnssecSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Dnssec, managedFilePath);
        var (_, dnssecCheckUnsignedSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DnssecCheckUnsigned, managedFilePath);
        var (_, readEthersSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.ReadEthers, managedFilePath);
        var (_, dhcpRapidCommitSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpRapidCommit, managedFilePath);
        var (_, tftpNoFailSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.TftpNoFail, managedFilePath);
        var (_, tftpNoBlocksizeSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.TftpNoBlocksize, managedFilePath);
        var (_, localmxSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Localmx, managedFilePath);
        var (_, selfmxSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Selfmx, managedFilePath);
        var (_, enableRaSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.EnableRa, managedFilePath);
        var (_, logDhcpSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.LogDhcp, managedFilePath);
        var (_, keepInForegroundSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.KeepInForeground, managedFilePath);
        var (_, noDaemonSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoDaemon, managedFilePath);
        var (_, proxyDnssecSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.ProxyDnssec, managedFilePath);
        var (_, connmarkAllowlistEnableSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.ConnmarkAllowlistEnable, managedFilePath);
        var (_, noRoundRobinSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoRoundRobin, managedFilePath);
        var (_, dnssecNoTimecheckSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DnssecNoTimecheck, managedFilePath);
        var (_, dnssecDebugSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DnssecDebug, managedFilePath);
        var leasequeryWithSource = (IReadOnlyList<(string Value, ConfigValueSource Source)>)ParseOptionWithSource(occurrences, DnsmasqConfKeys.Leasequery, managedFilePath);
        var (_, dhcpGenerateNamesSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpGenerateNames, managedFilePath);
        var (_, dhcpBroadcastSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpBroadcast, managedFilePath);
        var (_, dhcpSequentialIpSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpSequentialIp, managedFilePath);
        var (_, dhcpIgnoreClidSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpIgnoreClid, managedFilePath);
        var (_, bootpDynamicSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.BootpDynamic, managedFilePath);
        var (_, noPingSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.NoPing, managedFilePath);
        var (_, scriptArpSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.ScriptArp, managedFilePath);
        var (_, scriptOnRenewalSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.ScriptOnRenewal, managedFilePath);
        var (_, dhcpNoOverrideSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpNoOverride, managedFilePath);
        var (_, quietDhcpSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.QuietDhcp, managedFilePath);
        var (_, quietDhcp6Source) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.QuietDhcp6, managedFilePath);
        var (_, quietRaSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.QuietRa, managedFilePath);
        var (_, quietTftpSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.QuietTftp, managedFilePath);

        var (_, dhcpLeaseFilePathSource) = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigOccurrencesWithSource(occurrences, managedFilePath);
        var (_, cacheSizeSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.CacheSize, managedFilePath);
        var (_, portSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Port, managedFilePath);
        var (_, localTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.LocalTtl, managedFilePath);
        var (_, pidFilePathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.PidFile, managedFilePath);
        var (_, userSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.User, managedFilePath);
        var (_, groupSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Group, managedFilePath);
        var (_, logFacilitySource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.LogFacility, managedFilePath);
        var (_, logQueriesSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.LogQueries, managedFilePath);
        var (_, authTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.AuthTtl, managedFilePath);
        var (_, ednsPacketMaxSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.EdnsPacketMax, managedFilePath);
        var (_, queryPortSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.QueryPort, managedFilePath);
        var (_, portLimitSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.PortLimit, managedFilePath);
        var (_, minPortSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.MinPort, managedFilePath);
        var (_, maxPortSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.MaxPort, managedFilePath);
        var (_, logAsyncSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.LogAsync, managedFilePath);
        var (_, localServiceSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.LocalService, managedFilePath);
        var (_, dhcpLeaseMaxSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpLeaseMax, managedFilePath);
        var (_, negTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.NegTtl, managedFilePath);
        var (_, maxTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.MaxTtl, managedFilePath);
        var (_, maxCacheTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.MaxCacheTtl, managedFilePath);
        var (_, minCacheTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.MinCacheTtl, managedFilePath);
        var (_, dhcpTtlSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpTtl, managedFilePath);
        var (_, tftpRootPathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.TftpRoot, managedFilePath);
        var (_, pxePromptSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.PxePrompt, managedFilePath);
        var (_, enableDbusSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.EnableDbus, managedFilePath);
        var (_, enableUbusSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.EnableUbus, managedFilePath);
        var (_, fastDnsRetrySource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.FastDnsRetry, managedFilePath);
        var (_, dhcpScriptPathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpScript, managedFilePath);
        var (_, mxTargetSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.MxTarget, managedFilePath);
        var (_, dnsForwardMaxSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DnsForwardMax, managedFilePath);
        var (_, dumpfilePathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Dumpfile, managedFilePath);
        var (_, dumpmaskSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Dumpmask, managedFilePath);
        var (_, addCpeIdSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.AddCpeId, managedFilePath);
        var (_, dnssecTimestampSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DnssecTimestamp, managedFilePath);
        var (_, dnssecLimitsSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DnssecLimits, managedFilePath);
        var (_, dhcpAlternatePortSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpAlternatePort, managedFilePath);
        var (_, dhcpDuidSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpDuid, managedFilePath);
        var (_, dhcpLuascriptPathSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpLuascript, managedFilePath);
        var (_, dhcpScriptuserSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpScriptuser, managedFilePath);
        var (_, dhcpPxeVendorSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.DhcpPxeVendor, managedFilePath);
        var (_, useStaleCacheSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.UseStaleCache, managedFilePath);
        var (_, addMacSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.AddMac, managedFilePath);
        var (_, stripMacSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.StripMac, managedFilePath);
        var (_, addSubnetSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.AddSubnet, managedFilePath);
        var (_, stripSubnetSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.StripSubnet, managedFilePath);
        var (_, umbrellaSource) = ((string?, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Umbrella, managedFilePath);
        var (do0x20Set, do0x20EncodeSourceA) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Do0x20Encode, managedFilePath);
        var (no0x20Set, no0x20EncodeSourceB) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.No0x20Encode, managedFilePath);
        // no-0x20-encode overrides do-0x20-encode; source reflects the effective directive
        var do0x20EncodeSource = no0x20Set ? no0x20EncodeSourceB : (do0x20Set ? do0x20EncodeSourceA : null);
        var (_, conntrackSource) = ((bool, ConfigValueSource?))ParseOptionWithSource(occurrences, DnsmasqConfKeys.Conntrack, managedFilePath);

        return new EffectiveConfigSources(
            noHostsSource, addnHostsWithSource.Select(t => new PathWithSource(t.Path, t.Source)).ToList(), hostsdirPathSource,
            serverWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            localWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
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
            dhcpOptsdirWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            connmarkAllowlistWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            caaRecordWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dnsRrWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            synthDomainWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            authZoneWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            authSoaWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            authSecServersWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            authPeerWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpRelayWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpCircuitidWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpRemoteidWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpSubscridWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpProxyWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            tagIfWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            bridgeInterfaceWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            sharedNetworkWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpOptionPxeWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
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
            connmarkAllowlistEnableSource,
            noRoundRobinSource,
            dnssecNoTimecheckSource,
            dnssecDebugSource,
            leasequeryWithSource.Select(t => new ValueWithSource(t.Value, t.Source)).ToList(),
            dhcpGenerateNamesSource,
            dhcpBroadcastSource,
            dhcpSequentialIpSource,
            dhcpIgnoreClidSource,
            bootpDynamicSource,
            noPingSource,
            scriptArpSource,
            scriptOnRenewalSource,
            dhcpNoOverrideSource,
            quietDhcpSource,
            quietDhcp6Source,
            quietRaSource,
            quietTftpSource,
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
            dnsForwardMaxSource,
            dumpfilePathSource,
            dumpmaskSource,
            addCpeIdSource,
            dnssecTimestampSource,
            dnssecLimitsSource,
            dhcpAlternatePortSource,
            dhcpDuidSource,
            dhcpLuascriptPathSource,
            dhcpScriptuserSource,
            dhcpPxeVendorSource,
            useStaleCacheSource,
            addMacSource,
            stripMacSource,
            addSubnetSource,
            stripSubnetSource,
            umbrellaSource,
            do0x20EncodeSource,
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
            ServerValues: Array.Empty<string>(), LocalValues: Array.Empty<string>(), RevServerValues: Array.Empty<string>(), AddressValues: Array.Empty<string>(), Interfaces: Array.Empty<string>(),
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
            DhcpHostsfilePaths: Array.Empty<string>(), DhcpOptsfilePaths: Array.Empty<string>(), DhcpHostsdirPaths: Array.Empty<string>(), DhcpOptsdirPaths: Array.Empty<string>(),
            ConnmarkAllowlistValues: Array.Empty<string>(), CaaRecordValues: Array.Empty<string>(), DnsRrValues: Array.Empty<string>(), SynthDomainValues: Array.Empty<string>(),
            AuthZoneValues: Array.Empty<string>(), AuthSoaValues: Array.Empty<string>(), AuthSecServersValues: Array.Empty<string>(), AuthPeerValues: Array.Empty<string>(),
            DhcpRelayValues: Array.Empty<string>(), DhcpCircuitidValues: Array.Empty<string>(), DhcpRemoteidValues: Array.Empty<string>(), DhcpSubscridValues: Array.Empty<string>(), DhcpProxyValues: Array.Empty<string>(),
            TagIfValues: Array.Empty<string>(), BridgeInterfaceValues: Array.Empty<string>(), SharedNetworkValues: Array.Empty<string>(), DhcpOptionPxeValues: Array.Empty<string>(),
            ExpandHosts: false, BogusPriv: false, StrictOrder: false, AllServers: false, NoResolv: false, DomainNeeded: false, NoPoll: false,
            BindInterfaces: false, BindDynamic: false, NoNegcache: false, DnsLoopDetect: false, StopDnsRebind: false, RebindLocalhostOk: false, ClearOnReload: false,
            Filterwin2k: false, FilterA: false, FilterAaaa: false, LocaliseQueries: false, LogDebug: false, DhcpAuthoritative: false, LeasefileRo: false,
            EnableTftp: false, TftpSecure: false, TftpNoFail: false, TftpNoBlocksize: false, Dnssec: false, DnssecCheckUnsigned: null,
            ReadEthers: false, DhcpRapidCommit: false, Localmx: false, Selfmx: false, EnableRa: false, LogDhcp: false,
            KeepInForeground: false, NoDaemon: false, ProxyDnssec: false,
            ConnmarkAllowlistEnable: null, NoRoundRobin: false, DnssecNoTimecheck: false, DnssecDebug: false, LeasequeryValues: Array.Empty<string>(), DhcpGenerateNames: null, DhcpBroadcast: null, DhcpSequentialIp: false, DhcpIgnoreClid: false, BootpDynamic: null, NoPing: false, ScriptArp: false, ScriptOnRenewal: false, DhcpNoOverride: false, QuietDhcp: false, QuietDhcp6: false, QuietRa: false, QuietTftp: false,
            DhcpLeaseFilePath: null, CacheSize: null, Port: null, LocalTtl: null, PidFilePath: null, User: null, Group: null,
            LogFacility: null, LogQueries: null, AuthTtl: null, EdnsPacketMax: null, QueryPort: null, PortLimit: null, MinPort: null, MaxPort: null, LogAsync: null, LocalService: null,
            DhcpLeaseMax: null, NegTtl: null, MaxTtl: null, MaxCacheTtl: null, MinCacheTtl: null, DhcpTtl: null,
            TftpRootPath: null, PxePrompt: null, EnableDbus: null, EnableUbus: null, FastDnsRetry: null,
            DhcpScriptPath: null, MxTarget: null, DnsForwardMax: null, DumpfilePath: null, Dumpmask: null, AddCpeId: null, DnssecTimestamp: null, DnssecLimits: null, DhcpAlternatePort: null, DhcpDuid: null, DhcpLuascriptPath: null, DhcpScriptuser: null, DhcpPxeVendor: null,
            UseStaleCache: null, AddMac: null, StripMac: false, AddSubnet: null, StripSubnet: false, Umbrella: null, Do0x20EncodeState: ExplicitToggleState.Default, Conntrack: false
        );

    private static EffectiveConfigSources CreateDefaultEffectiveConfigSources() =>
        new(
            NoHosts: null, AddnHostsPaths: Array.Empty<PathWithSource>(), HostsdirPath: null,
            ServerValues: Array.Empty<ValueWithSource>(), LocalValues: Array.Empty<ValueWithSource>(), RevServerValues: Array.Empty<ValueWithSource>(), AddressValues: Array.Empty<ValueWithSource>(),
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
            DhcpHostsfilePaths: Array.Empty<ValueWithSource>(), DhcpOptsfilePaths: Array.Empty<ValueWithSource>(), DhcpHostsdirPaths: Array.Empty<ValueWithSource>(), DhcpOptsdirPaths: Array.Empty<ValueWithSource>(),
            ConnmarkAllowlistValues: Array.Empty<ValueWithSource>(), CaaRecordValues: Array.Empty<ValueWithSource>(), DnsRrValues: Array.Empty<ValueWithSource>(), SynthDomainValues: Array.Empty<ValueWithSource>(),
            AuthZoneValues: Array.Empty<ValueWithSource>(), AuthSoaValues: Array.Empty<ValueWithSource>(), AuthSecServersValues: Array.Empty<ValueWithSource>(), AuthPeerValues: Array.Empty<ValueWithSource>(),
            DhcpRelayValues: Array.Empty<ValueWithSource>(), DhcpCircuitidValues: Array.Empty<ValueWithSource>(), DhcpRemoteidValues: Array.Empty<ValueWithSource>(), DhcpSubscridValues: Array.Empty<ValueWithSource>(), DhcpProxyValues: Array.Empty<ValueWithSource>(),
            TagIfValues: Array.Empty<ValueWithSource>(), BridgeInterfaceValues: Array.Empty<ValueWithSource>(), SharedNetworkValues: Array.Empty<ValueWithSource>(), DhcpOptionPxeValues: Array.Empty<ValueWithSource>(),
            ExpandHosts: null, BogusPriv: null, StrictOrder: null, AllServers: null, NoResolv: null, DomainNeeded: null, NoPoll: null,
            BindInterfaces: null, BindDynamic: null, NoNegcache: null, DnsLoopDetect: null, StopDnsRebind: null, RebindLocalhostOk: null, ClearOnReload: null,
            Filterwin2k: null, FilterA: null, FilterAaaa: null, LocaliseQueries: null, LogDebug: null, DhcpAuthoritative: null, LeasefileRo: null,
            EnableTftp: null, TftpSecure: null, TftpNoFail: null, TftpNoBlocksize: null, Dnssec: null, DnssecCheckUnsigned: null,
            ReadEthers: null, DhcpRapidCommit: null, Localmx: null, Selfmx: null, EnableRa: null, LogDhcp: null,
            KeepInForeground: null, NoDaemon: null, ProxyDnssec: null,
            ConnmarkAllowlistEnable: null, NoRoundRobin: null, DnssecNoTimecheck: null, DnssecDebug: null, LeasequeryValues: Array.Empty<ValueWithSource>(), DhcpGenerateNames: null, DhcpBroadcast: null, DhcpSequentialIp: null, DhcpIgnoreClid: null, BootpDynamic: null, NoPing: null, ScriptArp: null, ScriptOnRenewal: null, DhcpNoOverride: null, QuietDhcp: null, QuietDhcp6: null, QuietRa: null, QuietTftp: null,
            DhcpLeaseFilePath: null, CacheSize: null, Port: null, LocalTtl: null, PidFilePath: null, User: null, Group: null,
            LogFacility: null, LogQueries: null, AuthTtl: null, EdnsPacketMax: null, QueryPort: null, PortLimit: null, MinPort: null, MaxPort: null, LogAsync: null, LocalService: null,
            DhcpLeaseMax: null, NegTtl: null, MaxTtl: null, MaxCacheTtl: null, MinCacheTtl: null, DhcpTtl: null,
            TftpRootPath: null, PxePrompt: null, EnableDbus: null, EnableUbus: null, FastDnsRetry: null,
            DhcpScriptPath: null, MxTarget: null, DnsForwardMax: null, DumpfilePath: null, Dumpmask: null, AddCpeId: null, DnssecTimestamp: null, DnssecLimits: null, DhcpAlternatePort: null, DhcpDuid: null, DhcpLuascriptPath: null, DhcpScriptuser: null, DhcpPxeVendor: null,
            UseStaleCache: null, AddMac: null, StripMac: null, AddSubnet: null, StripSubnet: null, Umbrella: null, Do0x20Encode: null, Conntrack: null
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
        foreach (var watcher in _includedFileWatchers.Values)
            watcher.Dispose();
        _includedFileWatchers.Clear();
    }
}
