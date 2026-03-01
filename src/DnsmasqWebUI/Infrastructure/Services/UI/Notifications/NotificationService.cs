using DnsmasqWebUI.Infrastructure.Services.UI.Notifications.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.UI.Notifications;

/// <inheritdoc cref="INotificationService" />
public sealed class NotificationService : INotificationService
{
    private readonly List<NotificationItem> _items = new();
    private int _nextId = 1;

    public event EventHandler? NotificationsChanged;

    public void ShowSuccess(string message) => Add(NotificationKind.Success, message);
    public void ShowError(string message) => Add(NotificationKind.Error, message);
    public void ShowWarning(string message) => Add(NotificationKind.Warning, message);
    public void ShowInfo(string message) => Add(NotificationKind.Info, message);

    public IReadOnlyList<NotificationItem> GetSnapshot()
    {
        lock (_items)
            return _items.ToList();
    }

    public void Dismiss(int id)
    {
        lock (_items)
        {
            var removed = _items.RemoveAll(i => i.Id == id);
            if (removed > 0)
                NotificationsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Add(NotificationKind kind, string message)
    {
        lock (_items)
        {
            _items.Add(new NotificationItem(_nextId++, kind, message ?? ""));
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
