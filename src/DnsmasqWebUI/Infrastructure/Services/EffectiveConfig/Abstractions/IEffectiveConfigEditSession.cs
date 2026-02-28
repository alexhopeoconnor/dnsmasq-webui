using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>
/// Scoped orchestrator for effective-config edit lifecycle: edit mode, active field, pending changes, and apply.
/// Components read state from here and call methods; the owning section re-renders after state changes.
/// </summary>
public interface IEffectiveConfigEditSession : IApplicationScopedService
{
    bool IsEditMode { get; }
    string? ActiveFieldKey { get; }
    IReadOnlyList<PendingEffectiveConfigChange> PendingChanges { get; }

    void EnterEditMode();
    void ExitEditModeDiscard();
    void ActivateField(string fieldKey);
    void DeactivateField();

    void TrackCommit(EffectiveConfigEditCommittedArgs args);
    void RevertChange(string sectionId, string optionName);

    Task ApplyAsync(CancellationToken ct = default);
}
