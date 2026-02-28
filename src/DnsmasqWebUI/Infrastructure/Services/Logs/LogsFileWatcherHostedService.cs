using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Logs.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services.Logs;

/// <summary>
/// When LogsPath is set in effective config, watches that file and triggers LogsService push on change.
/// Polling remains the primary mechanism; this is a best-effort optimization.
/// </summary>
public sealed class LogsFileWatcherHostedService : IApplicationHostedService
{
    private const int ResolveIntervalSeconds = 60;

    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly ILogsService _logsService;
    private readonly ILogger<LogsFileWatcherHostedService> _logger;
    private FileSystemWatcher? _watcher;
    private string? _currentPath;
    private readonly object _lock = new();

    public LogsFileWatcherHostedService(
        IDnsmasqConfigSetService configSetService,
        ILogsService logsService,
        ILogger<LogsFileWatcherHostedService> logger)
    {
        _configSetService = configSetService;
        _logsService = logsService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _ = RunLoopAsync(ct);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var (effectiveConfig, _) = _configSetService.GetEffectiveConfigWithSources();
                var logsPath = EffectiveDnsmasqConfig.GetLogsPath(effectiveConfig);
                UpdateWatcher(logsPath);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error resolving LogsPath for file watcher");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(ResolveIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        lock (_lock)
        {
            _watcher?.Dispose();
            _watcher = null;
            _currentPath = null;
        }
    }

    private void UpdateWatcher(string? logsPath)
    {
        var normalized = string.IsNullOrEmpty(logsPath) ? null : Path.GetFullPath(logsPath.Trim());

        lock (_lock)
        {
            if (string.Equals(_currentPath, normalized, StringComparison.Ordinal))
                return;

            _watcher?.Dispose();
            _watcher = null;
            _currentPath = normalized;

            if (string.IsNullOrEmpty(normalized))
            {
                _logger.LogDebug("Logs path not set; no file watcher");
                return;
            }

            var dir = Path.GetDirectoryName(normalized);
            var fileName = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(fileName))
            {
                _logger.LogDebug("Invalid logs path for watcher: {Path}", normalized);
                return;
            }

            try
            {
                if (!Directory.Exists(dir))
                {
                    _logger.LogDebug("Logs directory does not exist yet: {Dir}", dir);
                    return;
                }

                _watcher = new FileSystemWatcher(dir)
                {
                    Filter = fileName,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
                _watcher.Changed += OnFileChanged;
                _watcher.EnableRaisingEvents = true;
                _logger.LogDebug("Watching logs file: {Path}", normalized);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create file watcher for logs: {Path}", normalized);
            }
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _ = InvokePushAsync();
    }

    private async Task InvokePushAsync()
    {
        try
        {
            await _logsService.RunAndPushDnsmasqLogsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error pushing logs on file change");
        }
    }
}
