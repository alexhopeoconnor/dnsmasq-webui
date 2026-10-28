using DnsmasqWebUI.Infrastructure.Services.Common.Process.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Models.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload;

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

    public bool RestartCommandConfigured => !string.IsNullOrWhiteSpace(_options.RestartCommand);

    public bool ReloadCommandConfigured => !string.IsNullOrWhiteSpace(_options.ReloadCommand);

    /// <summary>
    /// Reloads dnsmasq's dynamic data (for example hosts files). This intentionally does not
    /// use RestartCommand as a fallback: callers that need a restart must opt into it explicitly.
    /// </summary>
    public Task<ReloadResult> ReloadAsync(CancellationToken ct = default) =>
        RunCommandAsync(_options.ReloadCommand, "reload", "ReloadCommand", ct);

    /// <summary>
    /// Restarts dnsmasq so it re-reads the complete configuration and include topology.
    /// ReloadCommand/SIGHUP is never used as a fallback here.
    /// </summary>
    public Task<ReloadResult> RestartAsync(CancellationToken ct = default) =>
        RunCommandAsync(_options.RestartCommand, "restart", "RestartCommand", ct);

    private async Task<ReloadResult> RunCommandAsync(
        string? command,
        string operation,
        string optionName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            var message = $"Dnsmasq:{optionName} is not configured.";
            _logger.LogWarning("{Operation} command not configured: {OptionName}", operation, optionName);
            return new ReloadResult(false, -1, null, message);
        }

        if (!await _reloadLock.WaitAsync(TimeSpan.Zero, ct))
        {
            _logger.LogDebug("Dnsmasq {Operation} already in progress, rejecting concurrent request", operation);
            return new ReloadResult(false, -1, null, $"Dnsmasq {operation} already in progress.");
        }

        try
        {
            var result = await _processRunner.RunAsync(command, _options.RestartTimeout, ct);
            var stderr = result.Stderr;
            if (result.TimedOut)
                stderr = (string.IsNullOrEmpty(stderr) ? "" : stderr + "\n") + $"Dnsmasq {operation} command timed out after {_options.RestartTimeoutSeconds} seconds.";
            if (result.ExceptionMessage != null)
                stderr = (string.IsNullOrEmpty(stderr) ? "" : stderr + "\n") + result.ExceptionMessage;

            if (result.ExitCode != 0 && result.ExitCode.HasValue)
                _logger.LogWarning("Dnsmasq {Operation} command exited with {ExitCode}: {Stderr}", operation, result.ExitCode.Value, stderr);
            else if (result.ExitCode == 0)
                _logger.LogInformation("Dnsmasq {Operation} command succeeded", operation);

            return new ReloadResult(
                result.ExitCode == 0,
                result.ExitCode ?? -1,
                result.Stdout,
                stderr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run dnsmasq {Operation} command", operation);
            return new ReloadResult(false, -1, null, ex.Message);
        }
        finally
        {
            _reloadLock.Release();
        }
    }
}
