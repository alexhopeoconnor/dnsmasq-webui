using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;

/// <summary>
/// Runs the configured dnsmasq apply commands. Reload is intentionally limited to
/// hosts data; a full restart is required for managed <c>.conf</c> changes.
/// </summary>
public interface IReloadService : IApplicationScopedService
{
    /// <summary>True when a full <see cref="RestartAsync"/> command is configured.</summary>
    bool RestartCommandConfigured { get; }

    /// <summary>True when a hosts-data <see cref="ReloadAsync"/> command is configured.</summary>
    bool ReloadCommandConfigured { get; }

    /// <summary>
    /// Executes the configured reload command (for example SIGHUP) and returns its result.
    /// This must not be used to apply <c>.conf</c> changes because dnsmasq does not re-read
    /// its include topology on SIGHUP.
    /// </summary>
    Task<ReloadResult> ReloadAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes the configured full restart command and returns its result. This does not
    /// fall back to <see cref="ReloadAsync"/> when no restart command is configured.
    /// </summary>
    Task<ReloadResult> RestartAsync(CancellationToken ct = default);
}

/// <summary>Result of running the reload command.</summary>
/// <param name="Success">True when the command exited with code 0.</param>
/// <param name="ExitCode">Process exit code; -1 when failed to start or timed out.</param>
/// <param name="StdOut">Standard output from the command.</param>
/// <param name="StdErr">Standard error from the command.</param>
public record ReloadResult(bool Success, int ExitCode, string? StdOut, string? StdErr);
