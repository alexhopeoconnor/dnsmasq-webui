using DnsmasqWebUI.Models.Client;

namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

/// <summary>
/// Scoped service to open the app-level settings modal from anywhere (e.g. NavMenu, pages).
/// MainLayout subscribes to <see cref="OpenRequested"/> and hosts the modal.
/// Subscribe to <see cref="SettingsChanged"/> to refresh when settings are saved/closed.
/// </summary>
public interface ISettingsModalService
{
    /// <summary>Raised when <see cref="Open"/> is called. MainLayout subscribes and shows the modal.</summary>
    event Action<SettingsModalContext, string>? OpenRequested;

    /// <summary>Raised when the modal is closed (save or cancel). Pages subscribe to reload their intervals.</summary>
    event Action? SettingsChanged;

    /// <summary>Opens the settings modal with the given context and title.</summary>
    void Open(SettingsModalContext context, string title);

    /// <summary>Called when the modal closes. Raises <see cref="SettingsChanged"/>.</summary>
    void NotifySettingsChanged();
}
