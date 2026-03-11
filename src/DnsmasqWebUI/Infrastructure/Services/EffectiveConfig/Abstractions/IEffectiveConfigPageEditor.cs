using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>
/// Page-facing orchestrator for specialized effective-config UI. Resolves descriptors from status,
/// overlays pending changes, and writes through the shared edit session so the section and widgets stay in sync.
/// </summary>
public interface IEffectiveConfigPageEditor : IApplicationScopedService
{
    void EnsureEditMode();

    EffectiveConfigFieldRef Field(string sectionId, string optionName);

    object? GetEffectiveValue(DnsmasqServiceStatus status, EffectiveConfigFieldRef field);
    IReadOnlyList<string> GetEffectiveMultiValues(DnsmasqServiceStatus status, EffectiveConfigFieldRef field);

    void SetSingleValue(DnsmasqServiceStatus status, EffectiveConfigFieldRef field, object? newValue);
    void ReplaceMultiValues(DnsmasqServiceStatus status, EffectiveConfigFieldRef field, IReadOnlyList<string> newValues);
    void AppendMultiValue(DnsmasqServiceStatus status, EffectiveConfigFieldRef field, string newValue);
    void RemoveMultiValue(DnsmasqServiceStatus status, EffectiveConfigFieldRef field, string valueToRemove);

    void Revert(EffectiveConfigFieldRef field);
    void SetFieldIssues(EffectiveConfigFieldRef field, IReadOnlyList<FieldIssue> issues);
    void ClearFieldIssues(EffectiveConfigFieldRef field);
    void RefreshCrossOptionIssues(DnsmasqServiceStatus status);
    void Activate(EffectiveConfigFieldRef field);
}
