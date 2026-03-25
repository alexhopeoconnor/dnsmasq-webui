using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>
/// Scoped orchestrator for effective-config edit lifecycle: edit mode, active field, pending changes, validation, and apply.
/// Components read state from here and call methods; the owning section re-renders after state changes.
/// </summary>
public interface IEffectiveConfigEditSession : IApplicationScopedService
{
    /// <summary>Raised when session state changes so UI (section and external widgets) can refresh.</summary>
    event Action? Changed;

    bool IsEditMode { get; }
    string? ActiveFieldKey { get; }
    IReadOnlyList<PendingDnsmasqChange> PendingChanges { get; }

    /// <summary>Per-field validation issues (errors block save; warnings can be confirmed).</summary>
    IReadOnlyDictionary<string, IReadOnlyList<FieldIssue>> FieldIssues { get; }

    void EnterEditMode();
    void ExitEditModeDiscard();
    void ActivateField(string fieldKey);
    void DeactivateField();

    void TrackCommit(EffectiveConfigEditCommittedArgs args);
    void RevertChange(string sectionId, string optionName);
    void TrackManagedHostsChange(PendingManagedHostsChange change);
    void RevertManagedHostsChange();

    void SetFieldIssues(string fieldKey, IReadOnlyList<FieldIssue> issues);
    void ClearFieldIssues(string fieldKey);

    /// <summary>Replaces all cross-option validation issues (e.g. no-resolv vs server). Merged with per-field issues for display and save guard.</summary>
    void SetCrossOptionIssues(IReadOnlyList<FieldIssue> issues);

    /// <summary>True if any field has one or more issues with <see cref="FieldIssueSeverity.Error"/>.</summary>
    bool HasBlockingValidationErrors();
    /// <summary>All issues across fields for summary display (e.g. toolbar count or save guard message).</summary>
    IReadOnlyList<FieldIssue> GetValidationSummary();

    Task<EffectiveConfigSaveResult> ApplyAsync(CancellationToken ct = default);

    /// <summary>
    /// Accepts that changes were applied to disk. Clears pending changes and optionally exits edit mode.
    /// Call this after a successful write (even if restart failed), so the session state matches disk state.
    /// </summary>
    /// <param name="stayInEditMode">If true, keep edit mode active; if false, exit edit mode.</param>
    void AcceptAppliedChanges(bool stayInEditMode);
}
