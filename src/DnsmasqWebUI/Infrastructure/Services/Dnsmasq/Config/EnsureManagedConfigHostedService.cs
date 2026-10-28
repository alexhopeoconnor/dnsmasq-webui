using System.Text;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;

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
        var configSetCache = scope.ServiceProvider.GetRequiredService<IConfigSetCache>();

        var set = await configSetService.GetConfigSetAsync(cancellationToken);
        var paths = DnsmasqManagedPathSet.TryFromOptions(_options);
        if (string.IsNullOrEmpty(set.ManagedFilePath) || paths == null)
            return;

        var mainFull = paths.MainConfigPath;
        var mainDir = Path.GetDirectoryName(mainFull) ?? "";
        var managedPath = paths.ManagedFilePath;
        var managedDir = paths.ManagedFilesDirectory;
        // Use absolute path so dnsmasq finds the file regardless of CWD (e.g. systemd may run with CWD=/).
        var confFileLine = "conf-file=" + managedPath;

        var mainFileBefore = await CaptureFileAsync(mainFull, cancellationToken);
        var managedConfigBefore = await CaptureFileAsync(managedPath, cancellationToken);
        var managedHostsBefore = await CaptureFileAsync(paths.ManagedHostsFilePath, cancellationToken);
        var originalLines = File.Exists(mainFull)
            ? (await File.ReadAllLinesAsync(mainFull, DnsmasqFileEncoding.Utf8NoBom, cancellationToken)).ToList()
            : new List<string>();
        DnsmasqFileEncoding.StripBomFromFirstLine(originalLines);
        var lines = originalLines.ToList();

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
        }

        var mainConfigChanged = !originalLines.SequenceEqual(lines, StringComparer.Ordinal);
        var managedConfigCreated = !managedConfigBefore.Exists;

        // dnsmasq reads .conf includes only at process start. Do not write an overlay that the
        // running daemon cannot be made to use: ReloadCommand/SIGHUP re-reads hosts data, not
        // the include topology we are about to change. This check deliberately happens before
        // creating a directory or changing either file so a misconfigured install stays intact.
        if ((mainConfigChanged || managedConfigCreated) && string.IsNullOrWhiteSpace(_options.RestartCommand))
        {
            const string message =
                "Cannot prepare the managed dnsmasq overlay: Dnsmasq:RestartCommand must be configured " +
                "because dnsmasq must restart to re-read .conf includes. No startup files were changed.";
            _logger.LogCritical(message);
            throw new InvalidOperationException(message);
        }

        if (mainConfigChanged)
        {
            await File.WriteAllLinesAsync(mainFull, lines, DnsmasqFileEncoding.Utf8NoBom, cancellationToken);
            // The initial config-set read above can have seen this include at a different position.
            // Do not rely on an asynchronous file watcher before the app begins serving requests.
            configSetCache.Invalidate();
            _logger.LogInformation("Set {Line} as the last line of main config {Path} so the managed file is included only by conf-file=", confFileLine, mainFull);
        }

        Directory.CreateDirectory(managedDir);

        if (managedConfigCreated)
        {
            _logger.LogInformation("Creating managed config file at startup: {Path}", set.ManagedFilePath);
            await configService.WriteManagedConfigAsync(Array.Empty<DnsmasqConfLine>(), cancellationToken);
        }
        var managedHostsCreated = !managedHostsBefore.Exists && File.Exists(paths.ManagedHostsFilePath);

        // The overlay can be created here after dnsmasq is already running (notably in the Docker
        // entrypoint), so make that transition explicit rather than leaving the UI's effective
        // view ahead of the running daemon. The preflight above guarantees RestartCommand exists.
        if (!mainConfigChanged && !managedConfigCreated)
            return;

        var reloadService = scope.ServiceProvider.GetRequiredService<IReloadService>();
        var restart = await reloadService.RestartAsync(cancellationToken);
        if (!restart.Success)
        {
            var detail = string.Join(Environment.NewLine, new[] { restart.StdOut, restart.StdErr }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
            _logger.LogError(
                "Managed dnsmasq overlay was updated at startup but RestartCommand failed (exit code {ExitCode}). {Detail}",
                restart.ExitCode,
                detail);
            var rollback = await TryRollbackStartupChangesAsync(
                mainFull,
                mainConfigChanged,
                mainFileBefore,
                managedPath,
                managedConfigCreated,
                managedConfigBefore,
                paths.ManagedHostsFilePath,
                managedHostsCreated,
                managedHostsBefore);
            configSetCache.Invalidate();
            var rollbackMessage = rollback.FilesRestored
                ? "The startup file changes were restored."
                : "The startup file changes could not be fully restored.";

            if (rollback.FilesRestored)
            {
                var rollbackRestart = await reloadService.RestartAsync(CancellationToken.None);
                if (rollbackRestart.Success)
                {
                    _logger.LogInformation("Restarted dnsmasq after restoring the previous startup overlay state.");
                    rollbackMessage += " dnsmasq was restarted with the restored state.";
                }
                else
                {
                    var rollbackDetail = string.Join(Environment.NewLine, new[] { rollbackRestart.StdOut, rollbackRestart.StdErr }
                        .Where(value => !string.IsNullOrWhiteSpace(value)));
                    _logger.LogError(
                        "dnsmasq could not be restarted after restoring the previous startup overlay state (exit code {ExitCode}). {Detail}",
                        rollbackRestart.ExitCode,
                        rollbackDetail);
                    rollbackMessage += " dnsmasq could not be restarted after restoration.";
                }
            }

            throw new InvalidOperationException(
                "Managed dnsmasq overlay was updated at startup, but dnsmasq could not be restarted. " +
                rollbackMessage + " The application is stopping rather than reporting an unapplied configuration. " +
                (string.IsNullOrWhiteSpace(detail) ? "" : detail));
        }

        _logger.LogInformation("Restarted dnsmasq after preparing the managed overlay at startup.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<StartupFileSnapshot> CaptureFileAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            return new StartupFileSnapshot(false, null);

        return new StartupFileSnapshot(true, await File.ReadAllBytesAsync(path, ct));
    }

    private async Task<(bool FilesRestored, string? Error)> TryRollbackStartupChangesAsync(
        string mainPath,
        bool mainChanged,
        StartupFileSnapshot mainBefore,
        string managedConfigPath,
        bool managedConfigCreated,
        StartupFileSnapshot managedConfigBefore,
        string managedHostsPath,
        bool managedHostsCreated,
        StartupFileSnapshot managedHostsBefore)
    {
        try
        {
            if (mainChanged)
                await RestoreFileAsync(mainPath, mainBefore, CancellationToken.None);
            if (managedConfigCreated)
                await RestoreFileAsync(managedConfigPath, managedConfigBefore, CancellationToken.None);
            if (managedHostsCreated)
                await RestoreFileAsync(managedHostsPath, managedHostsBefore, CancellationToken.None);

            _logger.LogWarning("Restored startup files after managed overlay restart failure.");
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Could not fully restore startup files after managed overlay restart failure.");
            return (false, ex.Message);
        }
    }

    private static async Task RestoreFileAsync(string path, StartupFileSnapshot before, CancellationToken ct)
    {
        if (!before.Exists)
        {
            if (File.Exists(path))
                File.Delete(path);
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(path, before.Content!, ct);
    }

    private sealed record StartupFileSnapshot(bool Exists, byte[]? Content);
}
