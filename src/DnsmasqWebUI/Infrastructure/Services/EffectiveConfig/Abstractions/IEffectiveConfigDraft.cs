using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>
/// Pending effective-config changes, validation issues, and apply/save. Independent of edit-mode UI state.
/// </summary>
public interface IEffectiveConfigDraft
{
    event Action? Changed;

    IReadOnlyList<PendingDnsmasqChange> PendingChanges { get; }
    IReadOnlyDictionary<string, IReadOnlyList<FieldIssue>> FieldIssues { get; }
    PendingManagedHostsChange? ManagedHostsDraft { get; }

    void TrackCommit(EffectiveConfigEditCommittedArgs args);
    void RevertChange(string sectionId, string optionName);

    void SetManagedHostsDraft(
        IReadOnlyList<HostEntry> baseline,
        IReadOnlyList<HostEntry> draft,
        string managedHostsFilePath);

    void RevertManagedHostsDraft();

    void SetFieldIssues(string fieldKey, IReadOnlyList<FieldIssue> issues);
    void ClearFieldIssues(string fieldKey);
    void SetCrossOptionIssues(IReadOnlyList<FieldIssue> issues);

    bool HasBlockingValidationErrors();
    IReadOnlyList<FieldIssue> GetValidationSummary();

    Task<EffectiveConfigSaveResult> ApplyAsync(CancellationToken ct = default);
    void AcceptAppliedChanges();
    void DiscardAllDraft();
}
