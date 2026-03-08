using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version;

/// <summary>At startup, verifies dnsmasq version meets minimum when <see cref="Models.Config.DnsmasqOptions.EnforceMinimumVersion"/> is true.</summary>
public sealed class DnsmasqVersionEnforcementHostedService : IApplicationHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<Models.Config.DnsmasqOptions> _options;

    public DnsmasqVersionEnforcementHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<Models.Config.DnsmasqOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var opts = _options.Value;
        if (!opts.EnforceMinimumVersion) return;

        using var scope = _scopeFactory.CreateScope();
        var versionService = scope.ServiceProvider.GetRequiredService<IDnsmasqVersionService>();
        var info = await versionService.GetVersionInfoAsync(ct);

        if (!info.ProbeSucceeded)
            throw new InvalidOperationException($"dnsmasq version probe failed: {info.Error}");

        if (!info.IsSupported)
            throw new InvalidOperationException(
                $"Installed dnsmasq version {info.InstalledVersion} is below minimum {info.MinimumVersion}.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
