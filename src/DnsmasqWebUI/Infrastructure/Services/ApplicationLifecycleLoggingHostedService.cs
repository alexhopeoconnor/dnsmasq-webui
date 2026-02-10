using System.Reflection;
using System.Runtime.InteropServices;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using DnsmasqWebUI.Models.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services;

/// <summary>Logs application start and stop with runtime and configuration context.</summary>
public sealed class ApplicationLifecycleLoggingHostedService : IApplicationHostedService
{
    private readonly ILogger<ApplicationLifecycleLoggingHostedService> _logger;
    private readonly IHostEnvironment _env;
    private readonly DnsmasqOptions _dnsmasq;
    private readonly DateTimeOffset _startedAt;

    public ApplicationLifecycleLoggingHostedService(
        ILogger<ApplicationLifecycleLoggingHostedService> logger,
        IHostEnvironment env,
        IOptions<DnsmasqOptions> dnsmasq)
    {
        _logger = logger;
        _env = env;
        _dnsmasq = dnsmasq.Value;
        _startedAt = DateTimeOffset.UtcNow;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var version = typeof(ApplicationLifecycleLoggingHostedService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        var mainDir = Path.GetDirectoryName(Path.GetFullPath(_dnsmasq.MainConfigPath)) ?? "";
        var managedConfigPath = Path.Combine(mainDir, _dnsmasq.ManagedFileName);
        var managedHostsPath = Path.Combine(mainDir, _dnsmasq.ManagedHostsFileName);

        _logger.LogInformation(
            "Application started: version={Version}, env={Environment}, os={OS}, arch={Arch}. " +
            "MainConfig={MainConfig}, managedConfig={ManagedConfig}, managedHosts={ManagedHosts}, systemHosts={SystemHosts}. " +
            "ReloadCommand={ReloadCmd}, StatusCommand={StatusCmd}",
            version,
            _env.EnvironmentName,
            RuntimeInformation.OSDescription.Trim(),
            RuntimeInformation.OSArchitecture,
            _dnsmasq.MainConfigPath,
            managedConfigPath,
            managedHostsPath,
            _dnsmasq.SystemHostsPath ?? "(not set)",
            string.IsNullOrWhiteSpace(_dnsmasq.ReloadCommand) ? "(not set)" : _dnsmasq.ReloadCommand,
            string.IsNullOrWhiteSpace(_dnsmasq.StatusCommand) ? "(not set)" : _dnsmasq.StatusCommand);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var uptime = DateTimeOffset.UtcNow - _startedAt;
        _logger.LogInformation("Application stopping: uptime={Uptime:F1}s", uptime.TotalSeconds);
        return Task.CompletedTask;
    }
}
