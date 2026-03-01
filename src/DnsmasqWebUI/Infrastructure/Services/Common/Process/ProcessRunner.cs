using System.Diagnostics;
using System.Threading.Channels;
using DnsmasqWebUI.Infrastructure.Services.Common.Process.Abstractions;
using DnsmasqWebUI.Models.Contracts;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services.Common.Process;

/// <summary>Runs shell commands via /bin/sh with async output capture and timeout. Supports run-to-completion and start-then-stream.</summary>
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

        try
        {
            await using var handle = await StartAsync(trimmed, maxOutputChars, ct);
            return await handle.WaitForExitAsync(timeout, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run command");
            return new ProcessRunResult(null, "", "", false, ex.Message);
        }
    }

    public async Task<IProcessHandle> StartAsync(string command, int? maxOutputChars = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command must be non-null and non-empty.", nameof(command));

        var trimmed = command.Trim();
        var prefix = trimmed.Length <= MaxCommandPrefixLength ? trimmed : trimmed[..MaxCommandPrefixLength] + "...";
        _logger.LogDebug("Starting command (length={Length}): {CommandPrefix}", trimmed.Length, prefix);

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = "-c \"" + trimmed.Replace("\"", "\\\"") + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var channel = Channel.CreateUnbounded<ProcessOutputLine>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });
        var handle = new ProcessHandle(process, channel, maxOutputChars, _logger);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return await Task.FromResult(handle);
    }
}
