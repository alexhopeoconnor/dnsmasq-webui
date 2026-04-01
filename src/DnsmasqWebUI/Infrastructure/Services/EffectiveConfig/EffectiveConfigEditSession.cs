using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

public sealed class EffectiveConfigEditSession : IEffectiveConfigEditSession
{
    private readonly EffectiveConfigDraft _draft;
    private readonly EffectiveConfigUiState _ui;

    public EffectiveConfigEditSession(IEffectiveConfigSaveService saveService)
    {
        _draft = new EffectiveConfigDraft(saveService);
        _ui = new EffectiveConfigUiState();
        _draft.Changed += OnChildChanged;
        _ui.Changed += OnChildChanged;
    }

    public event Action? Changed;

    public IEffectiveConfigDraft Draft => _draft;
    public IEffectiveConfigUiState Ui => _ui;

    private void OnChildChanged() => Changed?.Invoke();
}
