namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Result of the effective-config save flow (backup, write, validate, restart). Used by UI to drive state and show restore/continue options.</summary>
/// <param name="Backups">Backups created before write (one per target file touched). Empty when no write occurred or no backups were created.</param>
/// <param name="Saved">True when all managed files were written successfully.</param>
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
    IReadOnlyList<DnsmasqManagedBackup> Backups,
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
    /// <summary>True when at least one backup was created (for restore UI).</summary>
    public bool BackupCreated => Backups.Count > 0;
    /// <summary>Machine-readable error codes for <see cref="ErrorCode"/>.</summary>
    public static class ErrorCodes
    {
        public const string NoChanges = "no_changes";
        public const string MissingManagedPath = "missing_managed_path";
        public const string WriteFailed = "write_failed";
        /// <summary>Save blocked before write because one or more changed options require dnsmasq capabilities not present in this build.</summary>
        public const string UnsupportedCapabilities = "unsupported_capabilities";
        /// <summary>Save blocked before write because semantic validation failed (invalid values).</summary>
        public const string SemanticValidationFailed = "semantic_validation_failed";
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
        new(Array.Empty<DnsmasqManagedBackup>(), false, false, -1, null, null, false, -1, null, null, ErrorCodes.NoChanges, "No pending changes.");
}
