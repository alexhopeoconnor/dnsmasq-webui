using System.Threading.Channels;
using DnsmasqWebUI.Infrastructure.Services.Logs.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services.Logs;

/// <summary>
/// Listens for signals from AppLogsLoggerProvider; drains pending app logs and pushes via LogsService.
/// Event-driven: push happens when logs are written, with a short debounce to batch rapid writes.
/// </summary>
public sealed class AppLogsPushTriggeredHostedService : IApplicationHostedService
{
    private const int DebounceMs = 50;

    private readonly Channel<byte> _channel;
    private readonly ILogsService _logsService;
    private readonly ILogger<AppLogsPushTriggeredHostedService> _logger;
    private CancellationTokenSource? _stopCts;
    private Task? _runLoopTask;

    public AppLogsPushTriggeredHostedService(
        Channel<byte> appLogsPushChannel,
        ILogsService logsService,
        ILogger<AppLogsPushTriggeredHostedService> logger)
    {
        _channel = appLogsPushChannel;
        _logsService = logsService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _stopCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runLoopTask = RunLoopAsync(_stopCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        var stopCts = _stopCts;
        _stopCts = null;
        if (stopCts != null)
        {
            try { stopCts.Cancel(); }
            finally { stopCts.Dispose(); }
        }

        var runLoopTask = _runLoopTask;
        _runLoopTask = null;
        if (runLoopTask == null)
            return;

        try
        {
            await runLoopTask;
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var _ in _channel.Reader.ReadAllAsync(ct))
            {
                await Task.Delay(DebounceMs, ct);
                while (_channel.Reader.TryRead(out byte discarded)) { } // Drain extra signals during debounce
                try
                {
                    await _logsService.PushAppLogsPendingAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error pushing app logs");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown; ignore
        }
    }
}
