using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

public sealed class EffectiveConfigUiState : IEffectiveConfigUiState
{
    public event Action? Changed;

    public bool IsEditMode { get; private set; }
    public string? ActiveFieldKey { get; private set; }

    public void EnterEditMode()
    {
        IsEditMode = true;
        ActiveFieldKey = null;
        Changed?.Invoke();
    }

    public void ExitEditMode()
    {
        IsEditMode = false;
        ActiveFieldKey = null;
        Changed?.Invoke();
    }

    public void ActivateField(string fieldKey)
    {
        IsEditMode = true;
        ActiveFieldKey = fieldKey;
        Changed?.Invoke();
    }

    public void DeactivateField()
    {
        ActiveFieldKey = null;
        Changed?.Invoke();
    }
}
