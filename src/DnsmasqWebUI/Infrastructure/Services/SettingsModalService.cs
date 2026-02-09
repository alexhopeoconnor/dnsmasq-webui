using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using DnsmasqWebUI.Models.Client;

namespace DnsmasqWebUI.Infrastructure.Services;

/// <inheritdoc cref="ISettingsModalService" />
public sealed class SettingsModalService : ISettingsModalService
{
    public event Action<SettingsModalContext, string>? OpenRequested;
    public event Action? SettingsChanged;

    public void Open(SettingsModalContext context, string title) => OpenRequested?.Invoke(context, title);

    public void NotifySettingsChanged() => SettingsChanged?.Invoke();
}
