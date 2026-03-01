namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Result of restoring the managed config from a backup file, then reloading dnsmasq.</summary>
/// <param name="Restored">True when the backup was copied over the managed file.</param>
/// <param name="Reloaded">True when dnsmasq reload succeeded after restore.</param>
/// <param name="ReloadExitCode">Reload process exit code; -1 when not run or failed to start.</param>
/// <param name="ReloadStdErr">Standard error from reload command.</param>
/// <param name="UserMessage">Short message for the user.</param>
public record EffectiveConfigRestoreResult(
    bool Restored,
    bool Reloaded,
    int ReloadExitCode,
    string? ReloadStdErr,
    string? UserMessage);
