using System.Text;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Logging;
using DnsmasqWebUI.Infrastructure.Parsers;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services;

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
        // Use absolute path so dnsmasq finds the file regardless of CWD (e.g. systemd may run with CWD=/).
        var confFileLine = "conf-file=" + managedPath;

        var lines = File.Exists(mainFull)
            ? (await File.ReadAllLinesAsync(mainFull, DnsmasqFileEncoding.Utf8NoBom, cancellationToken)).ToList()
            : new List<string>();
        DnsmasqFileEncoding.StripBomFromFirstLine(lines);

        // Remove any conf-file= line that points to our managed path (we will add it as the last line).
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith("conf-file=", StringComparison.OrdinalIgnoreCase))
                continue;
            var value = trimmed.Length > 10 ? trimmed[10..].Trim() : "";
            if (string.IsNullOrEmpty(value))
                continue;
            // Resolve relative to main config dir; absolute value is used as-is.
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
            await File.WriteAllLinesAsync(mainFull, lines, DnsmasqFileEncoding.Utf8NoBom, cancellationToken);
            _logger.LogInformation(LogEvents.ManagedConfigConfFileLineSet, "Set {Line} as the last line of main config {Path} so the managed file is included only by conf-file=.", confFileLine, mainFull);
        }
        else
        {
            await File.WriteAllLinesAsync(mainFull, lines, DnsmasqFileEncoding.Utf8NoBom, cancellationToken);
        }

        Directory.CreateDirectory(managedDir);

        if (File.Exists(set.ManagedFilePath))
            return;

        _logger.LogInformation(LogEvents.ManagedConfigCreatedAtStartup, "Creating managed config file at startup: {Path}", set.ManagedFilePath);
        await configService.WriteManagedConfigAsync(Array.Empty<DnsmasqConfLine>(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
