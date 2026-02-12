namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

/// <summary>
/// Service that checks GitHub for a newer release. Runs on a configurable interval (hosted service)
/// and can be triggered manually. UI subscribes to <see cref="ResultChanged"/> to refresh when state updates.
/// </summary>
public interface IUpdateCheckService
{
    /// <summary>Raised when a check completes (background or manual). Subscribe and call StateHasChanged from the UI.</summary>
    event EventHandler? ResultChanged;

    /// <summary>True if the latest GitHub release is newer than the current app version.</summary>
    bool NewerVersionAvailable { get; }

    /// <summary>Tag of the newer release (e.g. v0.0.5), or null if none.</summary>
    string? NewerVersionTag { get; }

    /// <summary>URL of the newer release page, or null if none.</summary>
    string? NewerVersionUrl { get; }

    /// <summary>When the last check completed (success or failure), or null if never run.</summary>
    DateTime? LastCheckTime { get; }

    /// <summary>True while a check is in progress.</summary>
    bool CheckInProgress { get; }

    /// <summary>Run a check now (e.g. user clicked "check for updates").</summary>
    Task CheckNowAsync();
}
