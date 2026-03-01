using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.UI.Notifications.Abstractions;

/// <summary>Scoped service to show toast notifications (success, error, warning, info). NotificationHost in MainLayout subscribes and renders toasts. Registered via assembly scanning (<see cref="IApplicationScopedService"/>).</summary>
public interface INotificationService : IApplicationScopedService
{
    /// <summary>Raised when the list of toasts changes. NotificationHost subscribes and calls StateHasChanged.</summary>
    event EventHandler? NotificationsChanged;

    /// <summary>Shows a success toast (e.g. "Config saved and dnsmasq reloaded."). Auto-dismisses after default duration.</summary>
    void ShowSuccess(string message);

    /// <summary>Shows an error toast.</summary>
    void ShowError(string message);

    /// <summary>Shows a warning toast.</summary>
    void ShowWarning(string message);

    /// <summary>Shows an info toast.</summary>
    void ShowInfo(string message);

    /// <summary>Returns a snapshot of current toasts for rendering. Caller should re-render when <see cref="NotificationsChanged"/> fires.</summary>
    IReadOnlyList<NotificationItem> GetSnapshot();

    /// <summary>Removes the toast with the given id. Raises <see cref="NotificationsChanged"/>.</summary>
    void Dismiss(int id);
}

/// <summary>Kind of notification for styling and icon.</summary>
public enum NotificationKind
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>Single toast item.</summary>
/// <param name="Id">Unique id for dismiss.</param>
/// <param name="Kind">Success, Error, Warning, Info.</param>
/// <param name="Message">Text to show.</param>
public record NotificationItem(int Id, NotificationKind Kind, string Message);
