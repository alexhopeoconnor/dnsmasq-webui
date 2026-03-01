namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Result of restoring the managed config from a backup file, then running the restart command.</summary>
/// <param name="Restored">True when the backup was copied over the managed file.</param>
/// <param name="Restarted">True when the restart command succeeded after restore.</param>
/// <param name="RestartExitCode">Restart command exit code; -1 when not run or failed to start.</param>
/// <param name="RestartStdErr">Standard error from the restart command.</param>
/// <param name="UserMessage">Short message for the user.</param>
public record EffectiveConfigRestoreResult(
    bool Restored,
    bool Restarted,
    int RestartExitCode,
    string? RestartStdErr,
    string? UserMessage);
