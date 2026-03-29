using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

/// <summary>
/// Page-facing orchestrator: resolves descriptors via the shared provider, overlays pending changes,
/// and writes through the shared edit session. Runs cross-option evaluation after mutations.
/// </summary>
public sealed class EffectiveConfigPageEditor : IEffectiveConfigPageEditor
{
    private readonly IEffectiveConfigEditSession _session;
    private readonly IEffectiveConfigDescriptorProvider _descriptorProvider;
    private readonly IEffectiveConfigCrossOptionValidationService _crossOptionValidation;

    public EffectiveConfigPageEditor(
        IEffectiveConfigEditSession session,
        IEffectiveConfigDescriptorProvider descriptorProvider,
        IEffectiveConfigCrossOptionValidationService crossOptionValidation)
    {
        _session = session;
        _descriptorProvider = descriptorProvider ?? throw new ArgumentNullException(nameof(descriptorProvider));
        _crossOptionValidation = crossOptionValidation ?? throw new ArgumentNullException(nameof(crossOptionValidation));
    }

    public void EnsureEditMode()
    {
        if (!_session.IsEditMode)
            _session.EnterEditMode();
    }

    public EffectiveConfigFieldRef Field(string sectionId, string optionName)
        => EffectiveConfigFieldRef.For(sectionId, optionName);

    public object? GetEffectiveValue(DnsmasqServiceStatus status, EffectiveConfigFieldRef field)
    {
        var descriptor = ResolveDescriptor(status, field);
        if (descriptor == null) return null;

        var pending = GetPendingOptionChange(field);
        var fromDescriptor = descriptor.IsMultiValue
            ? (object?)(descriptor.GetItems()?.Select(i => i.Value).ToList() ?? new List<string>())
            : descriptor.GetValue();
        return pending != null ? pending.NewValue : fromDescriptor;
    }

    public IReadOnlyList<string> GetEffectiveMultiValues(DnsmasqServiceStatus status, EffectiveConfigFieldRef field)
    {
        var descriptor = ResolveDescriptor(status, field);
        if (descriptor == null) return new List<string>();

        var pending = GetPendingOptionChange(field);
        if (pending?.NewValue is IReadOnlyList<string> list)
            return list;
        return descriptor.GetItems()?.Select(i => i.Value).ToList() ?? new List<string>();
    }

    public void SetSingleValue(DnsmasqServiceStatus status, EffectiveConfigFieldRef field, object? newValue)
    {
        EnsureEditMode();
        var descriptor = ResolveDescriptor(status, field);
        if (descriptor == null) return;

        var pending = GetPendingOptionChange(field);
        var baseValue = descriptor.IsMultiValue
            ? (object?)(descriptor.GetItems()?.Select(i => i.Value).ToList() ?? new List<string>())
            : descriptor.GetValue();
        var oldValue = pending?.OldValue ?? baseValue;
        var source = descriptor.GetSource() ?? descriptor.GetItems()?.FirstOrDefault()?.Source;
        _session.TrackCommit(new EffectiveConfigEditCommittedArgs(
            field.SectionId, field.OptionName, oldValue, newValue, source?.FilePath));
        RefreshCrossOptionIssues(status);
    }

    public void ReplaceMultiValues(DnsmasqServiceStatus status, EffectiveConfigFieldRef field, IReadOnlyList<string> newValues)
    {
        EnsureEditMode();
        var descriptor = ResolveDescriptor(status, field);
        if (descriptor == null) return;

        var pending = GetPendingOptionChange(field);
        var baseValues = descriptor.GetItems()?.Select(i => i.Value).ToList() ?? new List<string>();
        var oldValues = pending?.OldValue is IReadOnlyList<string> list ? list.ToList() : baseValues;
        var source = descriptor.GetSource() ?? descriptor.GetItems()?.FirstOrDefault()?.Source;
        _session.TrackCommit(new EffectiveConfigEditCommittedArgs(
            field.SectionId, field.OptionName, oldValues, newValues.ToList(), source?.FilePath));
        RefreshCrossOptionIssues(status);
    }

    public void AppendMultiValue(DnsmasqServiceStatus status, EffectiveConfigFieldRef field, string newValue)
    {
        var current = GetEffectiveMultiValues(status, field);
        var next = current.Append(newValue).ToList();
        ReplaceMultiValues(status, field, next);
    }

    public void RemoveMultiValue(DnsmasqServiceStatus status, EffectiveConfigFieldRef field, string valueToRemove)
    {
        var current = GetEffectiveMultiValues(status, field);
        var next = current.Where(v => !string.Equals(v, valueToRemove, StringComparison.Ordinal)).ToList();
        ReplaceMultiValues(status, field, next);
    }

    public void Revert(EffectiveConfigFieldRef field)
    {
        _session.RevertChange(field.SectionId, field.OptionName);
    }

    public void SetFieldIssues(EffectiveConfigFieldRef field, IReadOnlyList<FieldIssue> issues)
    {
        _session.SetFieldIssues(field.FieldKey, issues);
    }

    public void ClearFieldIssues(EffectiveConfigFieldRef field)
    {
        _session.ClearFieldIssues(field.FieldKey);
    }

    public void RefreshCrossOptionIssues(DnsmasqServiceStatus status)
    {
        var issues = _crossOptionValidation.Validate(
            status,
            _session.PendingChanges.OfType<PendingOptionChange>().ToList());
        _session.SetCrossOptionIssues(issues);
    }

    public void Activate(EffectiveConfigFieldRef field)
    {
        EnsureEditMode();
        _session.ActivateField(field.FieldKey);
    }

    private PendingOptionChange? GetPendingOptionChange(EffectiveConfigFieldRef field)
    {
        return _session.PendingChanges.OfType<PendingOptionChange>().FirstOrDefault(c =>
            string.Equals(c.SectionId, field.SectionId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.OptionName, field.OptionName, StringComparison.OrdinalIgnoreCase));
    }

    private EffectiveConfigFieldDescriptor? ResolveDescriptor(DnsmasqServiceStatus status, EffectiveConfigFieldRef field)
    {
        return _descriptorProvider.Resolve(status, field);
    }
}
