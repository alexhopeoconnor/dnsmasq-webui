using System.Threading.Channels;
using DnsmasqWebUI.Infrastructure.Logging;
using DnsmasqWebUI.Infrastructure.Services;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using DnsmasqWebUI.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Extensions;

/// <summary>
/// App logs setup: buffer, channel, and provider.
/// Encapsulates the wiring that must run before the DI container exists (ILoggerProvider is created during host configuration).
/// Log level and excluded category prefixes are configured via appsettings.Overrides.json (config source with reloadOnChange).
/// </summary>
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Adds the app logs provider (forwards ILogger output to SignalR) and registers buffer and channel.
    /// Call before <c>builder.Build()</c>.
    /// </summary>
    public static ILoggingBuilder AddAppLogs(this ILoggingBuilder logging, IConfiguration configuration)
    {
        var services = logging.Services;

        var buffer = new AppLogsBuffer();
        var pushChannel = Channel.CreateBounded<byte>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

        services.AddSingleton(pushChannel);
        services.AddSingleton<IAppLogsBuffer>(buffer);
        services.AddSingleton<ILogsService, LogsService>();

        logging.AddProvider(new AppLogsLoggerProvider(buffer, pushChannel, configuration));

        return logging;
    }
}
