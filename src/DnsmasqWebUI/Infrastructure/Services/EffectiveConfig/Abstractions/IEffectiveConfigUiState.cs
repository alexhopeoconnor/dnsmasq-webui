namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>
/// Edit affordance state only: whether the effective-config UI is in edit mode and which field is active.
/// Does not own pending changes or validation.
/// </summary>
public interface IEffectiveConfigUiState
{
    event Action? Changed;

    bool IsEditMode { get; }
    string? ActiveFieldKey { get; }

    void EnterEditMode();
    void ExitEditMode();
    void ActivateField(string fieldKey);
    void DeactivateField();
}
