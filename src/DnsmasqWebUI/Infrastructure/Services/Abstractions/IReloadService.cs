namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

/// <summary>Runs the configured dnsmasq reload command (e.g. systemctl reload dnsmasq).</summary>
public interface IReloadService : IApplicationScopedService
{
    /// <summary>Executes the reload command and returns exit code and output. Serialised so only one reload runs at a time.</summary>
    Task<ReloadResult> ReloadAsync(CancellationToken ct = default);
}

/// <summary>Result of running the reload command.</summary>
/// <param name="Success">True when the command exited with code 0.</param>
/// <param name="ExitCode">Process exit code; -1 when failed to start or timed out.</param>
/// <param name="StdOut">Standard output from the command.</param>
/// <param name="StdErr">Standard error from the command.</param>
public record ReloadResult(bool Success, int ExitCode, string? StdOut, string? StdErr);
