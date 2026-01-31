using DnsmasqWebUI.Options;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

public class ReloadService : IReloadService
{
    private readonly DnsmasqOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<ReloadService> _logger;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public ReloadService(IProcessRunner processRunner, IOptions<DnsmasqOptions> options, ILogger<ReloadService> logger)
    {
        _processRunner = processRunner;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReloadResult> ReloadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ReloadCommand))
        {
            _logger.LogDebug("Reload command not configured");
            return new ReloadResult(true, 0, null, "Reload command not configured");
        }

        if (!await _reloadLock.WaitAsync(TimeSpan.Zero, ct))
        {
            _logger.LogDebug("Reload already in progress, rejecting concurrent request");
            return new ReloadResult(false, -1, null, "Reload already in progress.");
        }

        try
        {
            var result = await _processRunner.RunAsync(_options.ReloadCommand, TimeSpan.FromSeconds(30), ct);
            var stderr = result.Stderr;
            if (result.TimedOut)
                stderr = (string.IsNullOrEmpty(stderr) ? "" : stderr + "\n") + "Reload command timed out after 30 seconds.";
            if (result.ExceptionMessage != null)
                stderr = (string.IsNullOrEmpty(stderr) ? "" : stderr + "\n") + result.ExceptionMessage;

            if (result.ExitCode != 0 && result.ExitCode.HasValue)
                _logger.LogWarning("Reload command exited with {ExitCode}: {Stderr}", result.ExitCode.Value, stderr);
            else if (result.ExitCode == 0)
                _logger.LogInformation("Reload command succeeded");

            return new ReloadResult(
                result.ExitCode == 0,
                result.ExitCode ?? -1,
                result.Stdout,
                stderr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run reload command");
            return new ReloadResult(false, -1, null, ex.Message);
        }
        finally
        {
            _reloadLock.Release();
        }
    }
}
