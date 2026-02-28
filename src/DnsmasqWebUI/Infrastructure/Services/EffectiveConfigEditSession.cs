using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services;

public sealed class EffectiveConfigEditSession : IEffectiveConfigEditSession
{
    private readonly IDnsmasqConfigService _configService;
    private readonly List<PendingEffectiveConfigChange> _pending = new();

    public EffectiveConfigEditSession(IDnsmasqConfigService configService)
    {
        _configService = configService;
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
        if (!Equals(args.OldValue, args.NewValue))
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

    public async Task ApplyAsync(CancellationToken ct = default)
    {
        if (_pending.Count == 0) return;
        var changes = _pending.ToList();
        await _configService.ApplyEffectiveConfigChangesAsync(changes, ct);
        ExitEditModeDiscard();
    }
}
