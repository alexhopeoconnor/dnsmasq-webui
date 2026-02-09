using DnsmasqWebUI.Infrastructure.Logging;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services;

/// <summary>Logs application started and stopping so the app logs viewer shows lifecycle events.</summary>
public sealed class ApplicationLifecycleLoggingHostedService : IApplicationHostedService
{
    private readonly ILogger<ApplicationLifecycleLoggingHostedService> _logger;

    public ApplicationLifecycleLoggingHostedService(ILogger<ApplicationLifecycleLoggingHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogEvents.ApplicationStarted, "Application started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(LogEvents.ApplicationStopping, "Application stopping");
        return Task.CompletedTask;
    }
}
