namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Result of the effective-config save flow (backup, write, restart). Used by UI to drive state and show restore/continue options.</summary>
/// <param name="BackupCreated">True when a backup file was created before write.</param>
/// <param name="BackupPath">Path to the backup file when one was created; null otherwise.</param>
/// <param name="Saved">True when the managed config was written successfully.</param>
/// <param name="Restarted">True when the restart command succeeded after write (config changes require restart, not SIGHUP).</param>
/// <param name="RestartExitCode">Restart command process exit code; -1 when not run or failed to start.</param>
/// <param name="RestartStdOut">Standard output from the restart command.</param>
/// <param name="RestartStdErr">Standard error from the restart command.</param>
/// <param name="ErrorCode">Machine-readable code: no_changes, missing_managed_path, write_failed, restart_failed, or null when success.</param>
/// <param name="UserMessage">Short message for the user.</param>
public record EffectiveConfigSaveResult(
    bool BackupCreated,
    string? BackupPath,
    bool Saved,
    bool Restarted,
    int RestartExitCode,
    string? RestartStdOut,
    string? RestartStdErr,
    string? ErrorCode,
    string? UserMessage)
{
    /// <summary>Result when there are no pending changes to apply.</summary>
    public static EffectiveConfigSaveResult NoChanges() =>
        new(false, null, false, false, -1, null, null, "no_changes", "No pending changes.");
}
