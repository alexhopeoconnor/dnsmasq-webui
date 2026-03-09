namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Result of the effective-config save flow (backup, write, validate, restart). Used by UI to drive state and show restore/continue options.</summary>
/// <param name="BackupCreated">True when a backup file was created before write.</param>
/// <param name="BackupPath">Path to the backup file when one was created; null otherwise.</param>
/// <param name="Saved">True when the managed config was written successfully.</param>
/// <param name="Validated">True when validation was run and succeeded (or was skipped). When validation ran and failed, restart is not attempted.</param>
/// <param name="ValidationExitCode">Validation command exit code; -1 when not run or failed to start.</param>
/// <param name="ValidationStdOut">Standard output from the validation command.</param>
/// <param name="ValidationStdErr">Standard error from the validation command.</param>
/// <param name="Restarted">True when the restart command succeeded after write (and after validation if run).</param>
/// <param name="RestartExitCode">Restart command process exit code; -1 when not run or failed to start.</param>
/// <param name="RestartStdOut">Standard output from the restart command.</param>
/// <param name="RestartStdErr">Standard error from the restart command.</param>
/// <param name="ErrorCode">Machine-readable code: see <see cref="ErrorCodes"/>; null when success.</param>
/// <param name="UserMessage">Short message for the user.</param>
public record EffectiveConfigSaveResult(
    bool BackupCreated,
    string? BackupPath,
    bool Saved,
    bool Validated,
    int ValidationExitCode,
    string? ValidationStdOut,
    string? ValidationStdErr,
    bool Restarted,
    int RestartExitCode,
    string? RestartStdOut,
    string? RestartStdErr,
    string? ErrorCode,
    string? UserMessage)
{
    /// <summary>Machine-readable error codes for <see cref="ErrorCode"/>.</summary>
    public static class ErrorCodes
    {
        public const string NoChanges = "no_changes";
        public const string MissingManagedPath = "missing_managed_path";
        public const string WriteFailed = "write_failed";
        /// <summary>Save blocked before write because one or more changed options require dnsmasq capabilities not present in this build.</summary>
        public const string UnsupportedCapabilities = "unsupported_capabilities";
        public const string ValidateFailed = "validate_failed";
        public const string RestartFailed = "restart_failed";
        public const string UnsupportedVersion = "unsupported_version";
        /// <summary>Version probe failed (timeout, command missing, unparseable output). Distinct from <see cref="UnsupportedVersion"/> (probe succeeded but version below minimum).</summary>
        public const string VersionProbeFailed = "version_probe_failed";
    }

    /// <summary>True when config was written but validation failed (restart not attempted).</summary>
    public bool IsValidateFailed => ErrorCode == ErrorCodes.ValidateFailed;

    /// <summary>Result when there are no pending changes to apply.</summary>
    public static EffectiveConfigSaveResult NoChanges() =>
        new(false, null, false, false, -1, null, null, false, -1, null, null, ErrorCodes.NoChanges, "No pending changes.");
}
