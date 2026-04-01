using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

public sealed class EffectiveConfigDraft : IEffectiveConfigDraft
{
    private readonly IEffectiveConfigSaveService _saveService;
    private readonly List<PendingDnsmasqChange> _pending = new();
    private readonly Dictionary<string, List<FieldIssue>> _fieldIssues = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FieldIssue> _crossOptionIssues = new();

    public EffectiveConfigDraft(IEffectiveConfigSaveService saveService)
    {
        _saveService = saveService;
    }

    public event Action? Changed;

    private void NotifyChanged() => Changed?.Invoke();

    public IReadOnlyList<PendingDnsmasqChange> PendingChanges => _pending;

    public PendingManagedHostsChange? ManagedHostsDraft =>
        _pending.OfType<PendingManagedHostsChange>().FirstOrDefault();

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

    public void DiscardAllDraft()
    {
        _pending.Clear();
        _fieldIssues.Clear();
        _crossOptionIssues.Clear();
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
        NotifyChanged();
    }

    public void RevertChange(string sectionId, string optionName)
    {
        _pending.RemoveAll(c => c is PendingOptionChange o && o.SectionId == sectionId && o.OptionName == optionName);
        ClearFieldIssues($"{sectionId}:{optionName}");
        NotifyChanged();
    }

    public void SetManagedHostsDraft(
        IReadOnlyList<HostEntry> baseline,
        IReadOnlyList<HostEntry> draft,
        string managedHostsFilePath)
    {
        _pending.RemoveAll(c => c is PendingManagedHostsChange);
        var pending = new PendingManagedHostsChange(baseline, draft, managedHostsFilePath);
        if (pending.HasChanges)
            _pending.Add(pending);
        NotifyChanged();
    }

    public void RevertManagedHostsDraft()
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

    public void AcceptAppliedChanges()
    {
        _pending.Clear();
        _fieldIssues.Clear();
        _crossOptionIssues.Clear();
        NotifyChanged();
    }

    private static IReadOnlyList<string>? AsStringList(object? value) => value as IReadOnlyList<string>;

    private static bool ValuesEqual(object? oldValue, object? newValue)
    {
        var oldList = AsStringList(oldValue);
        var newList = AsStringList(newValue);
        if (oldList != null || newList != null)
            return (oldList ?? Array.Empty<string>()).SequenceEqual(newList ?? Array.Empty<string>(), StringComparer.Ordinal);
        return Equals(oldValue, newValue);
    }
}
