using System.Text;
using DnsmasqWebUI.Models;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Configuration;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

/// <summary>
/// Runs once at startup: ensures the managed file is included only by a conf-file= directive (never conf-dir=),
/// and that this conf-file= line is the absolute last line of the main config. Removes any existing
/// conf-file= that points to the managed path, trims trailing blank lines, then ensures the file ends with
/// exactly that conf-file= line. Creates the managed config file if it does not exist.
/// Requires write access to MainConfigPath and the managed file directory.
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
        var managedPath = Path.GetFullPath(set.ManagedFilePath);
        var managedDir = Path.GetDirectoryName(managedPath) ?? "";
        var confFileLine = "conf-file=" + Path.GetRelativePath(mainDir, managedPath);

        var lines = File.Exists(mainFull)
            ? (await File.ReadAllLinesAsync(mainFull, Encoding.UTF8, cancellationToken)).ToList()
            : new List<string>();

        // Remove any conf-file= line that points to our managed path (we will add it as the last line).
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith("conf-file=", StringComparison.OrdinalIgnoreCase))
                continue;
            var value = trimmed.Length > 10 ? trimmed[10..].Trim() : "";
            if (string.IsNullOrEmpty(value))
                continue;
            var resolved = Path.GetFullPath(Path.Combine(mainDir, value));
            if (string.Equals(resolved, managedPath, StringComparison.Ordinal))
                lines.RemoveAt(i);
        }

        // Trim trailing blank lines so the last line is the last non-blank content (or empty).
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
            lines.RemoveAt(lines.Count - 1);

        // Ensure the conf-file= line for the managed file is the absolute last line.
        if (lines.Count == 0 || !lines[^1].Trim().Equals(confFileLine, StringComparison.Ordinal))
        {
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                lines.Add("");
            lines.Add(confFileLine);
            await File.WriteAllLinesAsync(mainFull, lines, Encoding.UTF8, cancellationToken);
            _logger.LogInformation("Set {Line} as the last line of main config {Path} so the managed file is included only by conf-file=.", confFileLine, mainFull);
        }
        else
        {
            await File.WriteAllLinesAsync(mainFull, lines, Encoding.UTF8, cancellationToken);
        }

        Directory.CreateDirectory(managedDir);

        if (File.Exists(set.ManagedFilePath))
            return;

        _logger.LogInformation("Creating managed config file at startup: {Path}", set.ManagedFilePath);
        await configService.WriteManagedConfigAsync(Array.Empty<DnsmasqConfLine>(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
