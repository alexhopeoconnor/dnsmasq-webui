using DnsmasqWebUI.Infrastructure.Services.Common.Process.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version.Abstractions;
using DnsmasqWebUI.Models.Config;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version;

/// <summary>Readiness check for /healthz/ready: dnsmasq version meets minimum and, when StatusCommand is configured, dnsmasq is running.</summary>
public sealed class DnsmasqVersionHealthCheck : IHealthCheck
{
    private readonly IDnsmasqVersionService _versionService;
    private readonly IProcessRunner _processRunner;
    private readonly DnsmasqOptions _options;

    public DnsmasqVersionHealthCheck(
        IDnsmasqVersionService versionService,
        IProcessRunner processRunner,
        IOptions<DnsmasqOptions> options)
    {
        _versionService = versionService;
        _processRunner = processRunner;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var info = await _versionService.GetVersionInfoAsync(ct);

        if (!info.ProbeSucceeded)
            return HealthCheckResult.Unhealthy("dnsmasq version probe failed", data: new Dictionary<string, object> { ["error"] = info.Error ?? "" });

        if (!info.IsSupported)
            return HealthCheckResult.Unhealthy(
                $"dnsmasq version {info.InstalledVersion} is below minimum {info.MinimumVersion}",
                data: new Dictionary<string, object>
                {
                    ["installed"] = info.InstalledVersion?.ToString() ?? "",
                    ["minimum"] = info.MinimumVersion.ToString()
                });

        if (!string.IsNullOrWhiteSpace(_options.StatusCommand))
        {
            var statusResult = await _processRunner.RunAsync(_options.StatusCommand, _options.StatusTimeout, ct);
            var active = statusResult.ExitCode == 0 && !statusResult.TimedOut && statusResult.ExceptionMessage == null;
            if (!active)
                return HealthCheckResult.Unhealthy(
                    "dnsmasq is not running",
                    data: new Dictionary<string, object> { ["error"] = statusResult.ExceptionMessage ?? (statusResult.TimedOut ? "status command timed out" : "status command failed") });
        }

        return HealthCheckResult.Healthy($"dnsmasq {info.InstalledVersion} (minimum {info.MinimumVersion})");
    }
}
