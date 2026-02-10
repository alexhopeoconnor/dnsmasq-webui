using System.Diagnostics;
using System.Text;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using DnsmasqWebUI.Models.Contracts;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services;

/// <summary>Runs shell commands via /bin/sh with async output capture and timeout. Used by StatusController and ReloadService.</summary>
public sealed class ProcessRunner : IProcessRunner
{
    private const int MaxCommandPrefixLength = 80;

    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(ILogger<ProcessRunner> logger) => _logger = logger;

    public Task<ProcessRunResult> RunAsync(string? command, TimeSpan timeout, CancellationToken ct = default) =>
        RunAsync(command, timeout, null, ct);

    public async Task<ProcessRunResult> RunAsync(string? command, TimeSpan timeout, int? maxOutputChars, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new ProcessRunResult(null, "", "", false);

        var trimmed = command!.Trim();
        var prefix = trimmed.Length <= MaxCommandPrefixLength ? trimmed : trimmed[..MaxCommandPrefixLength] + "...";
        _logger.LogDebug("Running command (length={Length}, timeout={Timeout}s): {CommandPrefix}", trimmed.Length, timeout.TotalSeconds, prefix);

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

        var truncMsg = "\n\n(output truncated)\n";
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            if (maxOutputChars.HasValue && stdout.Length >= maxOutputChars.Value) return;
            stdout.AppendLine(e.Data);
            if (maxOutputChars.HasValue && stdout.Length > maxOutputChars.Value)
            {
                var keep = Math.Max(0, maxOutputChars.Value - truncMsg.Length);
                stdout.Length = keep;
                stdout.Append(truncMsg);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            if (maxOutputChars.HasValue && stderr.Length >= maxOutputChars.Value) return;
            stderr.AppendLine(e.Data);
            if (maxOutputChars.HasValue && stderr.Length > maxOutputChars.Value)
            {
                var keep = Math.Max(0, maxOutputChars.Value - truncMsg.Length);
                stderr.Length = keep;
                stderr.Append(truncMsg);
            }
        };

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

            var exitCode = process.HasExited ? process.ExitCode : -1;
            _logger.LogDebug("Command completed, exit code={ExitCode}", exitCode);
            return new ProcessRunResult(
                exitCode,
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
