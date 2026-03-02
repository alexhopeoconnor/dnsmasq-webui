namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Cascaded context for effective-config UI intents. Children call these methods instead of
/// receiving 6–8 event callbacks; the section (or shell) provides the implementation.
/// </summary>
public sealed class EffectiveConfigUiContext
{
    public required Func<ConfigOptionHelpRequestEventArgs, Task> RequestOptionHelpAsync { get; init; }
    public required Func<ReadonlyBadgeClickedEventArgs, Task> RequestReadonlyPopoverAsync { get; init; }
    public required Func<Task> ScheduleReadonlyPopoverCloseAsync { get; init; }
    public required Func<string, Task> ActivateFieldAsync { get; init; }
    public required Func<Task> DeactivateFieldAsync { get; init; }
    public required Func<EffectiveConfigEditCommittedArgs, Task> CommitFieldAsync { get; init; }
    /// <summary>Revert a pending change for the given field and refresh the section (e.g. when user toggles a flag back to its original value).</summary>
    public Func<string, string, Task>? RevertFieldAsync { get; init; }
}
