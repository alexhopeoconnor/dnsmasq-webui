using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

public sealed class EffectiveConfigEditSession : IEffectiveConfigEditSession
{
    private readonly IEffectiveConfigSaveService _saveService;
    private readonly List<PendingDnsmasqChange> _pending = new();
    private readonly Dictionary<string, List<FieldIssue>> _fieldIssues = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FieldIssue> _crossOptionIssues = new();

    public EffectiveConfigEditSession(IEffectiveConfigSaveService saveService)
    {
        _saveService = saveService;
    }

    public event Action? Changed;

    private void NotifyChanged() => Changed?.Invoke();

    public bool IsEditMode { get; private set; }
    public string? ActiveFieldKey { get; private set; }
    public IReadOnlyList<PendingDnsmasqChange> PendingChanges => _pending;
    public IReadOnlyDictionary<string, IReadOnlyList<FieldIssue>> FieldIssues
    {
        get
        {
            var copy = new Dictionary<string, List<FieldIssue>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _fieldIssues)
                copy[kv.Key] = new List<FieldIssue>(kv.Value);
            foreach (var issue in _crossOptionIssues)
            {
                if (!copy.TryGetValue(issue.FieldKey, out var list))
                    copy[issue.FieldKey] = list = new List<FieldIssue>();
                list.Add(issue);
            }
            return copy.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<FieldIssue>)kv.Value, StringComparer.OrdinalIgnoreCase);
        }
    }

    public void EnterEditMode()
    {
        IsEditMode = true;
        ActiveFieldKey = null;
        _pending.Clear();
        _fieldIssues.Clear();
        _crossOptionIssues.Clear();
        NotifyChanged();
    }

    public void ExitEditModeDiscard()
    {
        _pending.Clear();
        _fieldIssues.Clear();
        _crossOptionIssues.Clear();
        ActiveFieldKey = null;
        IsEditMode = false;
        NotifyChanged();
    }

    public void ActivateField(string fieldKey)
    {
        IsEditMode = true;
        ActiveFieldKey = fieldKey;
        NotifyChanged();
    }

    public void DeactivateField()
    {
        ActiveFieldKey = null;
        NotifyChanged();
    }

    public void SetFieldIssues(string fieldKey, IReadOnlyList<FieldIssue> issues)
    {
        if (string.IsNullOrEmpty(fieldKey)) return;
        _fieldIssues[fieldKey] = issues.ToList();
        NotifyChanged();
    }

    public void ClearFieldIssues(string fieldKey)
    {
        if (string.IsNullOrEmpty(fieldKey)) return;
        _fieldIssues.Remove(fieldKey);
        NotifyChanged();
    }

    public void SetCrossOptionIssues(IReadOnlyList<FieldIssue> issues)
    {
        _crossOptionIssues.Clear();
        if (issues != null)
            _crossOptionIssues.AddRange(issues);
        NotifyChanged();
    }

    public bool HasBlockingValidationErrors()
    {
        if (_fieldIssues.Values.Any(list => list.Any(i => i.Severity == FieldIssueSeverity.Error)))
            return true;
        return _crossOptionIssues.Any(i => i.Severity == FieldIssueSeverity.Error);
    }

    public IReadOnlyList<FieldIssue> GetValidationSummary()
    {
        var list = new List<FieldIssue>(_fieldIssues.Values.SelectMany(x => x));
        list.AddRange(_crossOptionIssues);
        return list;
    }

    public void TrackCommit(EffectiveConfigEditCommittedArgs args)
    {
        _pending.RemoveAll(c => c is PendingOptionChange o && o.SectionId == args.SectionId && o.OptionName == args.OptionName);
        if (!ValuesEqual(args.OldValue, args.NewValue))
            _pending.Add(new PendingOptionChange(
                args.SectionId, args.OptionName, args.OldValue, args.NewValue, args.CurrentSourceFilePath));
        ActiveFieldKey = null;
        NotifyChanged();
    }

    public void RevertChange(string sectionId, string optionName)
    {
        _pending.RemoveAll(c => c is PendingOptionChange o && o.SectionId == sectionId && o.OptionName == optionName);
        ClearFieldIssues($"{sectionId}:{optionName}");
        NotifyChanged();
    }

    public void TrackManagedHostsChange(PendingManagedHostsChange change)
    {
        _pending.RemoveAll(c => c is PendingManagedHostsChange);
        _pending.Add(change);
        NotifyChanged();
    }

    public void RevertManagedHostsChange()
    {
        _pending.RemoveAll(c => c is PendingManagedHostsChange);
        NotifyChanged();
    }

    public async Task<EffectiveConfigSaveResult> ApplyAsync(CancellationToken ct = default)
    {
        if (_pending.Count == 0)
            return EffectiveConfigSaveResult.NoChanges();

        return await _saveService.SaveAsync(_pending.ToList(), ct);
    }

    public void AcceptAppliedChanges(bool stayInEditMode)
    {
        _pending.Clear();
        _fieldIssues.Clear();
        _crossOptionIssues.Clear();
        ActiveFieldKey = null;
        IsEditMode = stayInEditMode;
        NotifyChanged();
    }

    private static IReadOnlyList<string>? AsStringList(object? value)
    {
        return value as IReadOnlyList<string>;
    }

    private static bool ValuesEqual(object? oldValue, object? newValue)
    {
        var oldList = AsStringList(oldValue);
        var newList = AsStringList(newValue);
        if (oldList != null || newList != null)
            return (oldList ?? Array.Empty<string>()).SequenceEqual(newList ?? Array.Empty<string>(), StringComparer.Ordinal);
        return Equals(oldValue, newValue);
    }
}
