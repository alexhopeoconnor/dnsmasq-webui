using System.Text;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts;

/// <summary>
/// Singleton cache for the app-managed hosts file and every hosts source resolved from the effective
/// dnsmasq configuration. The cache follows source-file and source-directory changes, and also compares
/// the current resolved source shape on every read so a missed file-system event cannot leave a directory
/// member list stale indefinitely.
/// </summary>
public sealed class HostsCache : IHostsCache, IDisposable
{
    private const int StaleCacheSeconds = 120;
    private const double SelfWriteIgnoreSeconds = 1.5;

    private readonly IConfigSetCache _configSetCache;
    private readonly IHostSourceResolver _sourceResolver;
    private readonly ILogger<HostsCache> _logger;
    private readonly object _lock = new();
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.Ordinal);
    private HostsSnapshot? _snapshot;
    private DateTime? _lastReadUtc;
    private bool _dirty = true;
    private string? _sourceFingerprint;
    private DateTime _lastWriteManagedUtc = DateTime.MinValue;
    private string? _lastManagedHostsPath;

    public HostsCache(
        IConfigSetCache configSetCache,
        IHostSourceResolver sourceResolver,
        ILogger<HostsCache> logger)
    {
        _configSetCache = configSetCache;
        _sourceResolver = sourceResolver;
        _logger = logger;
    }

    public void Invalidate()
    {
        lock (_lock)
            _dirty = true;
    }

    public void NotifyWeWroteManagedHosts(IReadOnlyList<HostEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        lock (_lock)
        {
            _lastWriteManagedUtc = DateTime.UtcNow;
            if (_snapshot == null)
                return;

            var managedEntries = entries.ToList();
            var sources = _snapshot.Sources
                .Select(source => source.SourceKind == HostsRowSourceKind.Managed
                    ? source with { EntryCount = managedEntries.Count }
                    : source)
                .ToList();
            _snapshot = _snapshot with { ManagedEntries = managedEntries, Sources = sources };
        }
    }

    public async Task<HostsSnapshot> GetSnapshotAsync(CancellationToken ct = default) =>
        await Task.Run(() => GetSnapshot(ct), ct);

    public async Task<IReadOnlyList<HostSource>> GetSourcesAsync(CancellationToken ct = default)
    {
        var snapshot = await GetSnapshotAsync(ct);
        return snapshot.Sources;
    }

    private HostsSnapshot GetSnapshot(CancellationToken ct)
    {
        var configSnapshot = _configSetCache.GetSnapshotAsync(ct).GetAwaiter().GetResult();
        return GetSnapshot(configSnapshot, ct);
    }

    /// <summary>
    /// Builds a hosts snapshot against one already-read config snapshot. Unified-row callers use this so
    /// source activation and effective-name expansion cannot be calculated from two different config reads.
    /// </summary>
    private HostsSnapshot GetSnapshot(ConfigSetSnapshot configSnapshot, CancellationToken ct)
    {
        var managedPath = configSnapshot.Set.ManagedHostsFilePath;
        var resolvedSources = _sourceResolver.Resolve(managedPath, configSnapshot.Config, ct).ToList();
        var sourceFingerprint = BuildSourceFingerprint(resolvedSources);

        lock (_lock)
        {
            if (!string.Equals(_sourceFingerprint, sourceFingerprint, StringComparison.Ordinal))
                _dirty = true;

            if (!_dirty && _snapshot != null && _lastReadUtc.HasValue)
            {
                var ageSeconds = (DateTime.UtcNow - _lastReadUtc.Value).TotalSeconds;
                if (ageSeconds < StaleCacheSeconds)
                    return _snapshot;
                _dirty = true;
            }

            ReconcileWatchers(resolvedSources);

            var (managedEntries, managedDiagnostic) = ReadManagedEntries(managedPath);
            var readOnlyFiles = new List<ReadOnlyHostsFile>();
            for (var i = 0; i < resolvedSources.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var source = resolvedSources[i];
                if (source.IsDirectory)
                    continue;

                if (source.SourceKind == HostsRowSourceKind.Managed)
                {
                    resolvedSources[i] = source with
                    {
                        EntryCount = managedEntries.Count,
                        Diagnostic = managedDiagnostic ?? source.Diagnostic
                    };
                    continue;
                }

                var (entries, diagnostic) = ReadEntries(source.Path, assignManagedIds: false);
                source = source with
                {
                    EntryCount = entries.Count,
                    Diagnostic = diagnostic ?? source.Diagnostic
                };
                resolvedSources[i] = source;
                readOnlyFiles.Add(new ReadOnlyHostsFile(
                    source.Path,
                    entries,
                    source.SourceKind,
                    source.IsActive,
                    source.InactiveReason,
                    source.LoadOrder,
                    source.ConfiguredPath,
                    source.Diagnostic));
            }

            _lastManagedHostsPath = TryGetFullPath(managedPath);
            _snapshot = new HostsSnapshot(managedEntries, readOnlyFiles)
            {
                Sources = resolvedSources
            };
            _sourceFingerprint = sourceFingerprint;
            _lastReadUtc = DateTime.UtcNow;
            _dirty = false;
            return _snapshot;
        }
    }

    private void ReconcileWatchers(IReadOnlyList<HostSource> sources)
    {
        var definitions = new Dictionary<string, WatchDefinition>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            if (source.IsDirectory)
            {
                AddDirectoryWatchDefinitions(definitions, source.Path);
            }
            else
            {
                AddFileWatchDefinition(definitions, source.Path);
            }
        }

        foreach (var stale in _watchers.Keys.Where(key => !definitions.ContainsKey(key)).ToList())
        {
            _watchers[stale].Dispose();
            _watchers.Remove(stale);
        }

        foreach (var definition in definitions.Values)
        {
            if (_watchers.ContainsKey(definition.Key))
                continue;

            try
            {
                var watcher = new FileSystemWatcher(definition.Directory, definition.Filter)
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.DirectoryName
                };
                watcher.Changed += OnFileSystemChanged;
                watcher.Created += OnFileSystemChanged;
                watcher.Deleted += OnFileSystemChanged;
                watcher.Renamed += OnFileSystemChanged;
                watcher.Error += OnWatcherError;
                watcher.EnableRaisingEvents = true;
                _watchers.Add(definition.Key, watcher);
                _logger.LogDebug("Watching hosts {WatchKind}: {Path}", definition.Kind, Path.Combine(definition.Directory, definition.Filter));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                _logger.LogWarning(ex, "Could not create hosts watcher for {Path}", Path.Combine(definition.Directory, definition.Filter));
            }
        }
    }

    private static void AddFileWatchDefinition(Dictionary<string, WatchDefinition> definitions, string path)
    {
        var fullPath = TryGetFullPath(path);
        if (fullPath == null)
            return;
        var directory = Path.GetDirectoryName(fullPath);
        var fileName = Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName) || !Directory.Exists(directory))
            return;

        var key = "file:" + fullPath;
        definitions.TryAdd(key, new WatchDefinition(key, directory, fileName, "file"));
    }

    private static void AddDirectoryWatchDefinitions(Dictionary<string, WatchDefinition> definitions, string path)
    {
        var fullPath = TryGetFullPath(path);
        if (fullPath == null)
            return;

        if (Directory.Exists(fullPath))
        {
            var contentsKey = "directory-contents:" + fullPath;
            definitions.TryAdd(contentsKey, new WatchDefinition(contentsKey, fullPath, "*", "directory contents"));
        }

        // Watch the parent as well so delete/rename/recreate of the configured directory itself invalidates us.
        var parent = Path.GetDirectoryName(fullPath);
        var name = Path.GetFileName(fullPath);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name) && Directory.Exists(parent))
        {
            var parentKey = "directory-parent:" + fullPath;
            definitions.TryAdd(parentKey, new WatchDefinition(parentKey, parent, name, "directory"));
        }
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        var fullPath = TryGetFullPath(e.FullPath);
        lock (_lock)
        {
            if (fullPath != null && _lastManagedHostsPath != null && PathsEqual(fullPath, _lastManagedHostsPath))
            {
                var elapsed = (DateTime.UtcNow - _lastWriteManagedUtc).TotalSeconds;
                if (elapsed < SelfWriteIgnoreSeconds)
                    return;
            }
            _dirty = true;
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogWarning(e.GetException(), "Hosts file watcher reported an error; the next read will refresh the snapshot.");
        Invalidate();
    }

    private (List<HostEntry> Entries, string? Diagnostic) ReadManagedEntries(string? managedPath) =>
        ReadEntries(managedPath, assignManagedIds: true);

    private static (List<HostEntry> Entries, string? Diagnostic) ReadEntries(string? path, bool assignManagedIds)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (new List<HostEntry>(), "File path is not configured.");

        var fullPath = TryGetFullPath(path);
        if (fullPath == null)
            return (new List<HostEntry>(), "File path is invalid.");
        if (!File.Exists(fullPath))
            return (new List<HostEntry>(), "File not found.");

        try
        {
            var lines = File.ReadAllLines(fullPath, Encoding.UTF8);
            var entries = new List<HostEntry>();
            var seenContentIds = assignManagedIds
                ? new HashSet<string>(StringComparer.Ordinal)
                : null;
            for (var i = 0; i < lines.Length; i++)
            {
                var entry = HostsFileLineParser.ParseLine(lines[i], i + 1);
                if (entry == null)
                    continue;

                if (assignManagedIds)
                    SetManagedEntryId(entry, seenContentIds!);
                entries.Add(entry);
            }
            return (entries, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DecoderFallbackException)
        {
            return (new List<HostEntry>(), "File could not be read.");
        }
    }

    private static void SetManagedEntryId(HostEntry entry, HashSet<string> seenContentIds)
    {
        if (entry.IsPassthrough || string.IsNullOrEmpty(entry.Address))
        {
            entry.Id = "line:" + entry.LineNumber;
            return;
        }

        var contentId = entry.Address + "|" + string.Join(",", entry.Names.OrderBy(x => x, StringComparer.Ordinal));
        entry.Id = seenContentIds.Add(contentId) ? contentId : contentId + ":" + entry.LineNumber;
    }

    /// <summary>
    /// Builds unified rows from the authoritative config snapshot. The old query arguments remain in the
    /// interface for compatibility with older browser clients but are deliberately ignored: clients must
    /// not decide which host sources are active or which path is managed.
    /// </summary>
    public async Task<IReadOnlyList<HostsPageRow>> GetUnifiedRowsAsync(
        bool expandHosts,
        bool noHosts,
        string? managedHostsPath,
        CancellationToken ct = default) =>
        await GetUnifiedRowsAsync(ct);

    /// <summary>Builds unified rows from the effective config and the resolved source inventory.</summary>
    public async Task<IReadOnlyList<HostsPageRow>> GetUnifiedRowsAsync(CancellationToken ct = default)
    {
        var configSnapshot = await _configSetCache.GetSnapshotAsync(ct);
        var snapshot = await Task.Run(() => GetSnapshot(configSnapshot, ct), ct);
        var config = configSnapshot.Config;
        var rows = new List<HostsPageRow>();
        var managedSource = snapshot.Sources
            .Where(s => s.SourceKind == HostsRowSourceKind.Managed)
            .OrderBy(s => s.LoadOrder)
            .FirstOrDefault();
        var managedPath = managedSource?.Path ?? configSnapshot.Set.ManagedHostsFilePath ?? "";
        var managedIsActive = managedSource?.IsActive ?? false;
        var managedInactiveReason = managedSource?.InactiveReason;

        foreach (var entry in snapshot.ManagedEntries)
        {
            if (entry.IsPassthrough)
                continue;
            rows.Add(ToRow(
                entry,
                id: entry.Id ?? $"managed:{entry.LineNumber}",
                sourceKind: HostsRowSourceKind.Managed,
                sourcePath: managedPath,
                isEditable: true,
                isActive: managedIsActive,
                inactiveReason: managedInactiveReason,
                config));
        }

        foreach (var file in snapshot.ReadOnlyFiles)
        {
            foreach (var entry in file.Entries)
            {
                if (entry.IsPassthrough)
                    continue;
                rows.Add(ToRow(
                    entry,
                    id: $"{file.SourceKind}:{file.LoadOrder}:{file.Path}:{entry.LineNumber}",
                    sourceKind: file.SourceKind,
                    sourcePath: file.Path,
                    isEditable: false,
                    isActive: file.IsActive,
                    inactiveReason: file.InactiveReason,
                    config));
            }
        }

        return rows;
    }

    private static HostsPageRow ToRow(
        HostEntry entry,
        string id,
        HostsRowSourceKind sourceKind,
        string sourcePath,
        bool isEditable,
        bool isActive,
        string? inactiveReason,
        DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig.EffectiveDnsmasqConfig config)
    {
        var names = entry.Names ?? (IReadOnlyList<string>)Array.Empty<string>();
        return new HostsPageRow(
            id,
            sourceKind,
            sourcePath,
            isEditable,
            isActive,
            inactiveReason,
            entry.Address ?? "",
            names,
            HostsEffectiveNames.Expand(names, config.ExpandHosts, config.DomainValues, entry.Address ?? ""),
            entry.IsComment,
            entry.LineNumber,
            entry.InlineComment);
    }

    private static string BuildSourceFingerprint(IReadOnlyList<HostSource> sources) =>
        string.Join("\u001f", sources.Select(source => string.Join("\u001e",
            (int)source.SourceKind,
            source.Path,
            source.ConfiguredPath,
            source.IsDirectory,
            source.IsActive,
            source.InactiveReason ?? "",
            source.LoadOrder,
            source.Diagnostic ?? "",
            GetSourceStamp(source))));

    /// <summary>
    /// Watchers are intentionally best-effort. A cheap metadata stamp makes ordinary reads self-healing
    /// when an editor saves by rename or a watcher drops an event.
    /// </summary>
    private static string GetSourceStamp(HostSource source)
    {
        try
        {
            if (source.IsDirectory)
            {
                var info = new DirectoryInfo(source.Path);
                return info.Exists ? $"directory:{info.LastWriteTimeUtc.Ticks}" : "directory:missing";
            }

            var file = new FileInfo(source.Path);
            return file.Exists ? $"file:{file.Length}:{file.LastWriteTimeUtc.Ticks}" : "file:missing";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return "unavailable";
        }
    }

    private static string? TryGetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var watcher in _watchers.Values)
                watcher.Dispose();
            _watchers.Clear();
        }
    }

    private sealed record WatchDefinition(string Key, string Directory, string Filter, string Kind);
}
