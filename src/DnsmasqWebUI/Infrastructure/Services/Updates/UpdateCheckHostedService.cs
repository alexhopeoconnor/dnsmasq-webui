using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Updates.Abstractions;
using DnsmasqWebUI.Models.Config;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Updates;

/// <summary>
/// Runs an update check shortly after startup, then periodically at the configured interval.
/// Disabled when <see cref="UpdateCheckOptions.IntervalMinutes"/> is 0.
/// </summary>
public sealed class UpdateCheckHostedService : IApplicationHostedService
{
    private const int StartupDelaySeconds = 30;

    private readonly IUpdateCheckService _updateCheckService;
    private readonly IOptions<UpdateCheckOptions> _options;

    public UpdateCheckHostedService(IUpdateCheckService updateCheckService, IOptions<UpdateCheckOptions> options)
    {
        _updateCheckService = updateCheckService;
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = RunLoopAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        var intervalMinutes = _options.Value.IntervalMinutes;
        if (intervalMinutes <= 0) return;

        await Task.Delay(TimeSpan.FromSeconds(StartupDelaySeconds), cancellationToken);
        await _updateCheckService.CheckNowAsync();

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), cancellationToken);
            await _updateCheckService.CheckNowAsync();
        }
    }
}
