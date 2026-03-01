using DnsmasqWebUI.Models.Contracts;

namespace DnsmasqWebUI.Infrastructure.Services.Common.Process.Abstractions;

/// <summary>
/// Handle to a started process. Call <see cref="ReadOutputAsync"/> for streaming output,
/// and/or <see cref="WaitForExitAsync"/> to wait for completion and get final result.
/// Disposing the handle kills the process if still running.
/// </summary>
public interface IProcessHandle : IAsyncDisposable
{
    /// <summary>Stream output lines as they are produced. Completes when the process exits and all output is read.</summary>
    IAsyncEnumerable<ProcessOutputLine> ReadOutputAsync(CancellationToken ct = default);

    /// <summary>Wait for the process to exit (or timeout). Returns final stdout/stderr and exit code. If timeout is set and exceeded, kills the process and returns with <see cref="ProcessRunResult.TimedOut"/> true.</summary>
    Task<ProcessRunResult> WaitForExitAsync(TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>Kill the process.</summary>
    Task KillAsync(CancellationToken ct = default);
}
