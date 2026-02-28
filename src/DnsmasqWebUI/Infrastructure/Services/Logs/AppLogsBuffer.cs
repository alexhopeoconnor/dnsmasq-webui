using DnsmasqWebUI.Infrastructure.Services.Logs.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.Logs;

/// <inheritdoc cref="IAppLogsBuffer" />
public sealed class AppLogsBuffer : IAppLogsBuffer
{
    private const int MaxLines = 1000;
    private readonly List<string> _lines = [];
    private readonly List<string> _pending = [];
    private readonly object _lock = new();

    public IReadOnlyList<string> GetRecent(int maxLines = 500)
    {
        lock (_lock)
        {
            if (_lines.Count <= maxLines)
                return _lines.ToList();
            return _lines.TakeLast(maxLines).ToList();
        }
    }

    public void Enqueue(string line)
    {
        lock (_lock)
        {
            _lines.Add(line);
            _pending.Add(line);
            while (_lines.Count > MaxLines)
                _lines.RemoveAt(0);
        }
    }

    public IReadOnlyList<string> DrainPending()
    {
        lock (_lock)
        {
            if (_pending.Count == 0)
                return [];
            var copy = _pending.ToList();
            _pending.Clear();
            return copy;
        }
    }
}
