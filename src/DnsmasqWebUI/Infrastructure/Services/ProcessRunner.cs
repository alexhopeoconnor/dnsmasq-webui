using System.Diagnostics;
using System.Text;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services;

/// <summary>Runs shell commands via /bin/sh with async output capture and timeout. Used by StatusController and ReloadService.</summary>
public sealed class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(ILogger<ProcessRunner> logger) => _logger = logger;

    public async Task<ProcessRunResult> RunAsync(string? command, TimeSpan timeout, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new ProcessRunResult(null, "", "", false);

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = "-c \"" + command.Replace("\"", "\\\"") + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                _logger.LogWarning("Command timed out after {Timeout}s", timeout.TotalSeconds);
                try { process.Kill(); } catch { /* best effort */ }
                var err = stderr.ToString();
                if (!string.IsNullOrEmpty(err)) err += "\n";
                err += $"Command timed out after {timeout.TotalSeconds} seconds.";
                return new ProcessRunResult(null, stdout.ToString(), err, true);
            }

            return new ProcessRunResult(
                process.HasExited ? process.ExitCode : -1,
                stdout.ToString(),
                stderr.ToString(),
                false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run command");
            return new ProcessRunResult(null, "", "", false, ex.Message);
        }
    }
}
