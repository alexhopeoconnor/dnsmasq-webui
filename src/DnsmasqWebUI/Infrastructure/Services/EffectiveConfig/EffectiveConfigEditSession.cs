using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

public sealed class EffectiveConfigEditSession : IEffectiveConfigEditSession
{
    private readonly IEffectiveConfigSaveService _saveService;
    private readonly List<PendingEffectiveConfigChange> _pending = new();

    public EffectiveConfigEditSession(IEffectiveConfigSaveService saveService)
    {
        _saveService = saveService;
    }

    public bool IsEditMode { get; private set; }
    public string? ActiveFieldKey { get; private set; }
    public IReadOnlyList<PendingEffectiveConfigChange> PendingChanges => _pending;

    public void EnterEditMode()
    {
        IsEditMode = true;
        ActiveFieldKey = null;
        _pending.Clear();
    }

    public void ExitEditModeDiscard()
    {
        _pending.Clear();
        ActiveFieldKey = null;
        IsEditMode = false;
    }

    public void ActivateField(string fieldKey)
    {
        IsEditMode = true;
        ActiveFieldKey = fieldKey;
    }

    public void DeactivateField()
    {
        ActiveFieldKey = null;
    }

    public void TrackCommit(EffectiveConfigEditCommittedArgs args)
    {
        var existing = _pending.FirstOrDefault(c =>
            c.SectionId == args.SectionId && c.OptionName == args.OptionName);
        if (existing != null)
            _pending.Remove(existing);
        if (!ValuesEqual(args.OldValue, args.NewValue))
            _pending.Add(new PendingEffectiveConfigChange(
                args.SectionId, args.OptionName, args.OldValue, args.NewValue, args.CurrentSourceFilePath));
        ActiveFieldKey = null;
    }

    public void RevertChange(string sectionId, string optionName)
    {
        var existing = _pending.FirstOrDefault(c =>
            c.SectionId == sectionId && c.OptionName == optionName);
        if (existing != null)
            _pending.Remove(existing);
    }

    public async Task<EffectiveConfigSaveResult> ApplyAsync(CancellationToken ct = default)
    {
        if (_pending.Count == 0)
            return EffectiveConfigSaveResult.NoChanges();

        var result = await _saveService.SaveAsync(_pending.ToList(), ct);
        if (result.Saved && result.Restarted)
            ExitEditModeDiscard();
        return result;
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
