using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>
/// Scoped root for effective-config editing: <see cref="Draft"/> holds pending changes and validation;
/// <see cref="Ui"/> holds edit mode and active field. Subscribe to <see cref="Changed"/> for any update.
/// </summary>
public interface IEffectiveConfigEditSession : IApplicationScopedService
{
    event Action? Changed;

    IEffectiveConfigDraft Draft { get; }
    IEffectiveConfigUiState Ui { get; }
}
