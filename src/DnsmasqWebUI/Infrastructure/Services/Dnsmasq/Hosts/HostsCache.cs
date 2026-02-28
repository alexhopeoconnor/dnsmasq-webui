using System.Text;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Infrastructure.Parsers;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts;

/// <summary>
/// Singleton cache for hosts: managed hosts file + read-only (system + addn-hosts). Invalidates on watchers (managed + system path), staleness, or Invalidate.
/// Call NotifyWeWroteManagedHosts after the app writes the managed hosts file so the cache updates in place and ignores the next watcher event.
/// </summary>
public sealed class HostsCache : IHostsCache, IDisposable
{
    private const int StaleCacheSeconds = 120;
    private const double SelfWriteIgnoreSeconds = 1.5;

    private readonly IConfigSetCache _configSetCache;
    private readonly DnsmasqOptions _options;
    private readonly ILogger<HostsCache> _logger;
    private readonly object _lock = new();
    private HostsSnapshot? _snapshot;
    private DateTime? _lastReadUtc;
    private bool _dirty = true;
    private DateTime _lastWriteManagedUtc = DateTime.MinValue;
    private string? _lastManagedHostsPath;
    private FileSystemWatcher? _watcherManaged;
    private FileSystemWatcher? _watcherSystem;

    public HostsCache(IConfigSetCache configSetCache, IOptions<DnsmasqOptions> options, ILogger<HostsCache> logger)
    {
        _configSetCache = configSetCache;
        _options = options.Value;
        _logger = logger;
    }

    public void Invalidate()
    {
        lock (_lock)
            _dirty = true;
    }

    public void NotifyWeWroteManagedHosts(IReadOnlyList<HostEntry> entries)
    {
        lock (_lock)
        {
            _lastWriteManagedUtc = DateTime.UtcNow;
            if (_snapshot != null)
                _snapshot = _snapshot with { ManagedEntries = entries.ToList() };
        }
    }

    public async Task<HostsSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        return await Task.Run(() => GetSnapshot(ct), ct);
    }

    private HostsSnapshot GetSnapshot(CancellationToken ct)
    {
        var configSnapshot = _configSetCache.GetSnapshotAsync(ct).GetAwaiter().GetResult();
        var set = configSnapshot.Set;
        var effectiveConfig = configSnapshot.Config;
        var managedPath = set.ManagedHostsFilePath;
        var addnPaths = effectiveConfig.AddnHostsPaths ?? Array.Empty<string>();
        var noHosts = effectiveConfig.NoHosts;

        lock (_lock)
        {
            if (!_dirty && _snapshot != null && _lastReadUtc.HasValue)
            {
                var ageSeconds = (DateTime.UtcNow - _lastReadUtc.Value).TotalSeconds;
                if (ageSeconds < StaleCacheSeconds)
                    return _snapshot;
                _dirty = true;
            }

            TryEnsureWatchers(managedPath);

            var managedEntries = ReadManagedEntries(managedPath);
            var readOnlyFiles = ReadReadOnlyFiles(managedPath, addnPaths, noHosts, ct);

            _lastManagedHostsPath = !string.IsNullOrEmpty(managedPath) ? Path.GetFullPath(managedPath) : null;
            _snapshot = new HostsSnapshot(managedEntries, readOnlyFiles);
            _lastReadUtc = DateTime.UtcNow;
            _dirty = false;
            return _snapshot;
        }
    }

    private void TryEnsureWatchers(string? managedPath)
    {
        if (_watcherManaged != null && _watcherSystem != null)
            return;

        var managedPathFull = !string.IsNullOrEmpty(managedPath) ? Path.GetFullPath(managedPath) : null;
        if (!string.IsNullOrEmpty(managedPathFull))
        {
            var dir = Path.GetDirectoryName(managedPathFull);
            var fileName = Path.GetFileName(managedPathFull);
            if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(fileName) && Directory.Exists(dir))
            {
                try
                {
                    _watcherManaged ??= CreateWatcher(dir, fileName, OnFileChanged);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not create watcher for managed hosts: {Path}", managedPathFull);
                }
            }
        }

        var systemPath = _options.SystemHostsPath?.Trim();
        var systemPathFull = !string.IsNullOrEmpty(systemPath) ? Path.GetFullPath(systemPath) : null;
        if (!string.IsNullOrEmpty(systemPathFull))
        {
            var dir = Path.GetDirectoryName(systemPathFull);
            var fileName = Path.GetFileName(systemPathFull);
            if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(fileName) && Directory.Exists(dir))
            {
                try
                {
                    _watcherSystem ??= CreateWatcher(dir, fileName, OnFileChanged);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not create watcher for system hosts: {Path}", systemPathFull);
                }
            }
        }
    }

    private static FileSystemWatcher CreateWatcher(string dir, string fileName, FileSystemEventHandler onChanged)
    {
        var w = new FileSystemWatcher(dir)
        {
            Filter = fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
        };
        w.Changed += onChanged;
        w.Created += onChanged;
        w.EnableRaisingEvents = true;
        return w;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var fullPath = Path.GetFullPath(e.FullPath);
        lock (_lock)
        {
            if (_lastManagedHostsPath != null && string.Equals(fullPath, _lastManagedHostsPath, StringComparison.Ordinal))
            {
                var elapsed = (DateTime.UtcNow - _lastWriteManagedUtc).TotalSeconds;
                if (elapsed < SelfWriteIgnoreSeconds)
                    return;
            }
            _dirty = true;
        }
    }

    private static List<HostEntry> ReadManagedEntries(string? managedPath)
    {
        if (string.IsNullOrEmpty(managedPath) || !File.Exists(managedPath))
            return new List<HostEntry>();

        var lines = File.ReadAllLines(managedPath, Encoding.UTF8);
        var entries = new List<HostEntry>();
        var seenContentIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < lines.Length; i++)
        {
            var entry = HostsFileLineParser.ParseLine(lines[i], i + 1);
            if (entry == null) continue;
            if (entry.IsPassthrough || string.IsNullOrEmpty(entry.Address))
                entry.Id = "line:" + entry.LineNumber;
            else
            {
                var contentId = entry.Address + "|" + string.Join(",", entry.Names.OrderBy(x => x, StringComparer.Ordinal));
                entry.Id = seenContentIds.Add(contentId) ? contentId : contentId + ":" + entry.LineNumber;
            }
            entries.Add(entry);
        }
        return entries;
    }

    private List<ReadOnlyHostsFile> ReadReadOnlyFiles(string? managedPath, IReadOnlyList<string> addnPaths, bool noHosts, CancellationToken ct)
    {
        var result = new List<ReadOnlyHostsFile>();
        var managedPathFull = !string.IsNullOrEmpty(managedPath) ? Path.GetFullPath(managedPath) : null;
        var systemPath = _options.SystemHostsPath?.Trim();
        var systemPathFull = !string.IsNullOrEmpty(systemPath) ? Path.GetFullPath(systemPath) : null;

        if (!noHosts && !string.IsNullOrEmpty(systemPathFull) && File.Exists(systemPathFull))
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var lines = File.ReadAllLines(systemPathFull, Encoding.UTF8);
                var entries = new List<HostEntry>();
                for (var i = 0; i < lines.Length; i++)
                {
                    var entry = HostsFileLineParser.ParseLine(lines[i], i + 1);
                    if (entry != null)
                        entries.Add(entry);
                }
                result.Add(new ReadOnlyHostsFile(systemPathFull, entries));
            }
            catch
            {
                // Skip unreadable
            }
        }

        foreach (var p in addnPaths)
        {
            ct.ThrowIfCancellationRequested();
            var fullPath = Path.GetFullPath(p);
            if (managedPathFull != null && string.Equals(fullPath, managedPathFull, StringComparison.Ordinal))
                continue;
            if (systemPathFull != null && string.Equals(fullPath, systemPathFull, StringComparison.Ordinal))
                continue;
            if (!File.Exists(fullPath))
                continue;
            try
            {
                var lines = File.ReadAllLines(fullPath, Encoding.UTF8);
                var entries = new List<HostEntry>();
                for (var i = 0; i < lines.Length; i++)
                {
                    var entry = HostsFileLineParser.ParseLine(lines[i], i + 1);
                    if (entry != null)
                        entries.Add(entry);
                }
                result.Add(new ReadOnlyHostsFile(fullPath, entries));
            }
            catch
            {
                // Skip unreadable
            }
        }

        return result;
    }

    public void Dispose()
    {
        _watcherManaged?.Dispose();
        _watcherManaged = null;
        _watcherSystem?.Dispose();
        _watcherSystem = null;
    }
}
