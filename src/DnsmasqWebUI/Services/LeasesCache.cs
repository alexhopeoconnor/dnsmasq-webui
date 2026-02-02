using System.Text;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;

namespace DnsmasqWebUI.Services;

/// <summary>
/// Singleton cache for the leases file with a <see cref="FileSystemWatcher"/> so we re-read only when the file changes.
/// </summary>
public sealed class LeasesCache : ILeasesCache, IDisposable
{
    private readonly string? _path;
    private readonly ILogger<LeasesCache> _logger;
    private FileSystemWatcher? _watcher;
    private readonly object _lock = new();
    private (bool Available, IReadOnlyList<LeaseEntry>? Entries)? _cache;
    private bool _dirty = true;

    public LeasesCache(IDnsmasqConfigSetService configSetService, ILogger<LeasesCache> logger)
    {
        _path = configSetService.GetLeasesPath();
        _logger = logger;
        if (string.IsNullOrEmpty(_path))
        {
            _logger.LogDebug("Leases path not configured; no file watcher");
            return;
        }
        var dir = Path.GetDirectoryName(_path);
        var fileName = Path.GetFileName(_path);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(fileName))
        {
            _logger.LogDebug("Invalid leases path for watcher: {Path}", _path);
            return;
        }
        try
        {
            if (!Directory.Exists(dir))
            {
                _logger.LogDebug("Leases directory does not exist yet: {Dir}", dir);
                return;
            }
            _watcher = new FileSystemWatcher(dir)
            {
                Filter = fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.EnableRaisingEvents = true;
            _logger.LogDebug("Watching leases file: {Path}", _path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create file watcher for leases: {Path}", _path);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            _dirty = true;
        }
    }

    public (bool Available, IReadOnlyList<LeaseEntry>? Entries) GetOrRefresh(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_path))
            return (false, null);

        lock (_lock)
        {
            if (!_dirty && _cache.HasValue)
                return _cache.Value;

            if (!File.Exists(_path))
            {
                _logger.LogDebug("Leases file not found: {Path}", _path);
                _cache = (true, Array.Empty<LeaseEntry>());
                _dirty = false;
                return _cache.Value;
            }
            try
            {
                var lines = File.ReadAllLines(_path, Encoding.UTF8);
                var entries = new List<LeaseEntry>();
                foreach (var line in lines)
                {
                    var entry = DnsmasqLeasesFileLineParser.ParseLine(line);
                    if (entry != null)
                        entries.Add(entry);
                }
                _cache = (true, entries);
                _dirty = false;
                return _cache.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read leases file: {Path}", _path);
                _cache = (true, null);
                _dirty = false;
                return _cache.Value;
            }
        }
    }

    public async Task<(bool Available, IReadOnlyList<LeaseEntry>? Entries)> GetOrRefreshAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_path))
            return (false, null);

        // Run file I/O on thread pool to avoid blocking
        return await Task.Run(() => GetOrRefresh(ct), ct);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
