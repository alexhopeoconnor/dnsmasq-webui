using System.Threading.Channels;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Logging;

/// <summary>
/// ILoggerProvider that forwards log entries to IAppLogsBuffer for real-time display.
/// Signals the push channel when logs are written so the hosted service can push to clients.
/// Filter is evaluated at Log() time (not CreateLogger) so runtime config changes take effect despite LoggerFactory caching.
/// </summary>
public sealed class AppLogsLoggerProvider : ILoggerProvider
{
    private readonly IAppLogsBuffer _buffer;
    private readonly Channel<byte> _pushChannel;
    private readonly IConfiguration _configuration;

    public AppLogsLoggerProvider(
        IAppLogsBuffer buffer,
        Channel<byte> pushChannel,
        IConfiguration configuration)
    {
        _buffer = buffer;
        _pushChannel = pushChannel;
        _configuration = configuration;
    }

    public ILogger CreateLogger(string categoryName) =>
        new FilteringAppLogsLogger(categoryName, _buffer, _pushChannel, _configuration);

    public void Dispose() { }

    private sealed class FilteringAppLogsLogger : ILogger
    {
        private readonly string _category;
        private readonly IAppLogsBuffer _buffer;
        private readonly Channel<byte> _pushChannel;
        private readonly IConfiguration _configuration;

        public FilteringAppLogsLogger(string category, IAppLogsBuffer buffer, Channel<byte> pushChannel, IConfiguration configuration)
        {
            _category = category;
            _buffer = buffer;
            _pushChannel = pushChannel;
            _configuration = configuration;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsExcluded(_category))
                return;

            var msg = formatter(state, exception);
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = logLevel switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "???"
            };
            var line = $"{timestamp} [{level}] {_category}: {msg}";
            if (exception != null)
                line += $"{Environment.NewLine}{exception}";
            _buffer.Enqueue(line);
            _pushChannel.Writer.TryWrite(0);
        }

        private bool IsExcluded(string category)
        {
            var prefixes = AppLogsConfigHelper.GetEffectiveExcludedPrefixes(_configuration);
            if (prefixes.Count == 0) return false;
            foreach (var prefix in prefixes)
            {
                if (!string.IsNullOrEmpty(prefix) && category.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
