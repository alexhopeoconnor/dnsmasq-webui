namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Result of the effective-config save flow (backup, write, reload). Used by UI to drive state and show restore/continue options.</summary>
/// <param name="BackupCreated">True when a backup file was created before write.</param>
/// <param name="BackupPath">Path to the backup file when one was created; null otherwise.</param>
/// <param name="Saved">True when the managed config was written successfully.</param>
/// <param name="Reloaded">True when dnsmasq reload succeeded after write.</param>
/// <param name="ReloadExitCode">Reload process exit code; -1 when not run or failed to start.</param>
/// <param name="ReloadStdOut">Standard output from reload command.</param>
/// <param name="ReloadStdErr">Standard error from reload command.</param>
/// <param name="ErrorCode">Machine-readable code: no_changes, missing_managed_path, write_failed, reload_failed, or null when success.</param>
/// <param name="UserMessage">Short message for the user.</param>
public record EffectiveConfigSaveResult(
    bool BackupCreated,
    string? BackupPath,
    bool Saved,
    bool Reloaded,
    int ReloadExitCode,
    string? ReloadStdOut,
    string? ReloadStdErr,
    string? ErrorCode,
    string? UserMessage)
{
    /// <summary>Result when there are no pending changes to apply.</summary>
    public static EffectiveConfigSaveResult NoChanges() =>
        new(false, null, false, false, -1, null, null, "no_changes", "No pending changes.");
}
