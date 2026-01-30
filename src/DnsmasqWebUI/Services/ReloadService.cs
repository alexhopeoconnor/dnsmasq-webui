using System.Diagnostics;
using DnsmasqWebUI.Options;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

public class ReloadService : IReloadService
{
    private readonly string? _command;
    private readonly ILogger<ReloadService> _logger;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public ReloadService(IOptions<DnsmasqOptions> options, ILogger<ReloadService> logger)
    {
        _command = options.Value.ReloadCommand;
        _logger = logger;
    }

    public async Task<ReloadResult> ReloadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_command))
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
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = "-c \"" + _command.Replace("\"", "\\\"") + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

            var timedOut = false;
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                timedOut = true;
                _logger.LogWarning("Reload command timed out after 30 seconds");
                try { process.Kill(); } catch { /* best effort */ }
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var exitCode = process.HasExited ? process.ExitCode : -1;
            if (timedOut)
                stderr = (string.IsNullOrEmpty(stderr) ? "" : stderr + "\n") + "Reload command timed out after 30 seconds.";
            if (exitCode != 0)
                _logger.LogWarning("Reload command exited with {ExitCode}: {Stderr}", exitCode, stderr);
            else
                _logger.LogInformation("Reload command succeeded");
            return new ReloadResult(exitCode == 0, exitCode, stdout, stderr);
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
