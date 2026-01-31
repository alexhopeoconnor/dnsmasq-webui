using System.Text;
using DnsmasqWebUI.Models;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Options;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

/// <summary>
/// Runs once at startup: ensures the main dnsmasq config ends with a conf-file= line for the managed file
/// (appends it at the end if missing), then creates the managed config file if it does not exist.
/// Requires write access to MainConfigPath and the managed file directory (e.g. root or a user that owns those paths).
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
        if (string.IsNullOrEmpty(set.ManagedFilePath) || string.IsNullOrEmpty(_options.MainConfigPath))
            return;

        var mainFull = Path.GetFullPath(_options.MainConfigPath);
        var mainDir = Path.GetDirectoryName(mainFull) ?? "";
        var managedDir = Path.Combine(mainDir, "dnsmasq.d");
        var managedPath = Path.Combine(managedDir, _options.ManagedFileName);
        var confFileLine = "conf-file=" + Path.Combine("dnsmasq.d", _options.ManagedFileName);

        var lines = File.Exists(mainFull)
            ? (await File.ReadAllLinesAsync(mainFull, Encoding.UTF8, cancellationToken)).ToList()
            : new List<string>();

        var toRemove = new List<int>();
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith("conf-file=", StringComparison.OrdinalIgnoreCase))
                continue;
            var value = trimmed.Length > 10 ? trimmed[10..].Trim() : "";
            if (string.IsNullOrEmpty(value))
                continue;
            var resolved = Path.GetFullPath(Path.Combine(mainDir, value));
            if (string.Equals(resolved, managedPath, StringComparison.Ordinal))
                toRemove.Add(i);
        }

        foreach (var i in toRemove.OrderByDescending(x => x))
            lines.RemoveAt(i);

        var endsWithConfFile = lines.Count > 0 && lines[^1].Trim().Equals(confFileLine, StringComparison.Ordinal);
        if (!endsWithConfFile)
        {
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                lines.Add("");
            lines.Add(confFileLine);
            await File.WriteAllLinesAsync(mainFull, lines, Encoding.UTF8, cancellationToken);
            _logger.LogInformation("Appended {Line} to end of main config {Path} so the managed file is included.", confFileLine, mainFull);
        }

        Directory.CreateDirectory(managedDir);

        if (File.Exists(set.ManagedFilePath))
            return;

        _logger.LogInformation("Creating managed config file at startup: {Path}", set.ManagedFilePath);
        await configService.WriteManagedConfigAsync(Array.Empty<DnsmasqConfLine>(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
