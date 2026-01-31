using System.Text;
using DnsmasqWebUI.Models;
using DnsmasqWebUI.Options;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

/// <summary>
/// Runs once at startup: ensures the main dnsmasq config includes the managed file (appends conf-dir= at the end if missing),
/// then creates the managed config file if it does not exist. Requires write access to MainConfigPath and the conf-dir
/// (e.g. root or a user that owns those paths; in Docker with host dnsmasq, the container typically runs as root when bind-mounting /etc/dnsmasq.d).
/// </summary>
public class EnsureManagedConfigHostedService : IApplicationHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnsureManagedConfigHostedService> _logger;
    private readonly DnsmasqOptions _options;

    public EnsureManagedConfigHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<DnsmasqOptions> options,
        ILogger<EnsureManagedConfigHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var configSetService = scope.ServiceProvider.GetRequiredService<IDnsmasqConfigSetService>();
        var configService = scope.ServiceProvider.GetRequiredService<IDnsmasqConfigService>();

        var set = await configSetService.GetConfigSetAsync(cancellationToken);

        if (string.IsNullOrEmpty(set.ManagedFilePath) && !string.IsNullOrEmpty(_options.MainConfigPath))
        {
            var mainFull = Path.GetFullPath(_options.MainConfigPath);
            var mainDir = Path.GetDirectoryName(mainFull) ?? "";
            var defaultConfDir = Path.Combine(mainDir, "dnsmasq.d");
            var lineToAppend = "conf-dir=" + defaultConfDir;

            var lines = File.Exists(mainFull)
                ? (await File.ReadAllLinesAsync(mainFull, Encoding.UTF8, cancellationToken)).ToList()
                : new List<string>();
            var trimmed = lines.Select(l => l.Trim()).ToList();
            var alreadyHasConfDir = trimmed.Any(l =>
                l.StartsWith("conf-dir=", StringComparison.OrdinalIgnoreCase) ||
                l.StartsWith("conf-file=", StringComparison.OrdinalIgnoreCase));
            if (!alreadyHasConfDir)
            {
                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                    lines.Add("");
                lines.Add(lineToAppend);
                await File.WriteAllLinesAsync(mainFull, lines, Encoding.UTF8, cancellationToken);
                _logger.LogInformation("Appended {Line} to main config {Path} so the managed file is included.", lineToAppend, mainFull);
            }
            Directory.CreateDirectory(defaultConfDir);
            set = await configSetService.GetConfigSetAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(set.ManagedFilePath))
            return;
        if (File.Exists(set.ManagedFilePath))
            return;

        _logger.LogInformation("Creating managed config file at startup: {Path}", set.ManagedFilePath);
        await configService.WriteManagedConfigAsync(Array.Empty<DnsmasqConfLine>(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
