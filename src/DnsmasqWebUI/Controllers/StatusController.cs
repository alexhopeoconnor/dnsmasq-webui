using DnsmasqWebUI.Models;
using DnsmasqWebUI.Configuration;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly DnsmasqOptions _options;
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly IProcessRunner _processRunner;

    public StatusController(
        IOptions<DnsmasqOptions> options,
        IDnsmasqConfigSetService configSetService,
        IProcessRunner processRunner)
    {
        _options = options.Value;
        _configSetService = configSetService;
        _processRunner = processRunner;
    }

    [HttpGet]
    public async Task<ActionResult<DnsmasqServiceStatus>> Get(CancellationToken ct)
    {
        try
        {
            var set = await _configSetService.GetConfigSetAsync(ct);
            var effectiveLeasesPath = _configSetService.GetLeasesPath();
            var effectiveConfig = _configSetService.GetEffectiveConfig();
            var systemHostsPath = _options.SystemHostsPath?.Trim();

            var statusResult = await _processRunner.RunAsync(_options.StatusCommand, TimeSpan.FromSeconds(5), ct);
            var dnsmasqStatus = statusResult.ExitCode == 0 ? "active" : (statusResult.ExitCode.HasValue ? "inactive" : "unknown");
            var statusCommandStdout = string.IsNullOrWhiteSpace(statusResult.Stdout) ? null : statusResult.Stdout.Trim();
            var statusCommandStderr = string.IsNullOrWhiteSpace(statusResult.Stderr) ? null : statusResult.Stderr.Trim();
            if (statusResult.ExceptionMessage != null)
                statusCommandStderr = (statusCommandStderr ?? "") + (statusCommandStderr != null ? "\n" : "") + statusResult.ExceptionMessage;

            var showTask = string.IsNullOrWhiteSpace(_options.StatusShowCommand)
                ? Task.FromResult(new ProcessRunResult(null, "", "", false))
                : _processRunner.RunAsync(_options.StatusShowCommand, TimeSpan.FromSeconds(5), ct);
            var logsTask = string.IsNullOrWhiteSpace(_options.LogsCommand)
                ? Task.FromResult(new ProcessRunResult(null, "", "", false))
                : _processRunner.RunAsync(_options.LogsCommand, TimeSpan.FromSeconds(10), ct);
            await Task.WhenAll(showTask, logsTask);

            var showResult = await showTask;
            var logsResult = await logsTask;
            var statusShowOutput = !string.IsNullOrWhiteSpace(_options.StatusShowCommand)
                ? showResult.Stdout + (showResult.TimedOut ? "\n(Command timed out.)" : "")
                : null;
            var logsOutput = !string.IsNullOrWhiteSpace(_options.LogsCommand)
                ? logsResult.Stdout + (logsResult.TimedOut ? "\n(Command timed out.)" : "")
                : null;

            var status = new DnsmasqServiceStatus(
                SystemHostsPath: string.IsNullOrEmpty(systemHostsPath) ? null : systemHostsPath,
                SystemHostsPathExists: !string.IsNullOrEmpty(systemHostsPath) && System.IO.File.Exists(systemHostsPath),
                NoHosts: effectiveConfig.NoHosts,
                AddnHostsPaths: effectiveConfig.AddnHostsPaths,
                EffectiveConfig: effectiveConfig,
                MainConfigPath: _options.MainConfigPath,
                ManagedFilePath: set.ManagedFilePath,
                LeasesPath: effectiveLeasesPath,
                MainConfigPathExists: !string.IsNullOrEmpty(_options.MainConfigPath) && System.IO.File.Exists(_options.MainConfigPath),
                ManagedFilePathExists: !string.IsNullOrEmpty(set.ManagedFilePath) && System.IO.File.Exists(set.ManagedFilePath),
                LeasesPathConfigured: !string.IsNullOrEmpty(effectiveLeasesPath),
                LeasesPathExists: !string.IsNullOrEmpty(effectiveLeasesPath) && System.IO.File.Exists(effectiveLeasesPath),
                ReloadCommandConfigured: !string.IsNullOrWhiteSpace(_options.ReloadCommand),
                StatusCommandConfigured: !string.IsNullOrWhiteSpace(_options.StatusCommand),
                StatusShowConfigured: !string.IsNullOrWhiteSpace(_options.StatusShowCommand),
                LogsConfigured: !string.IsNullOrWhiteSpace(_options.LogsCommand),
                LogsPath: EffectiveDnsmasqConfig.GetLogsPath(effectiveConfig),
                StatusShowCommand: string.IsNullOrWhiteSpace(_options.StatusShowCommand) ? null : _options.StatusShowCommand!.Trim(),
                LogsCommand: string.IsNullOrWhiteSpace(_options.LogsCommand) ? null : _options.LogsCommand!.Trim(),
                DnsmasqStatus: dnsmasqStatus,
                StatusCommandExitCode: dnsmasqStatus != "active" && statusResult.ExitCode.HasValue ? statusResult.ExitCode.Value : null,
                StatusCommandStdout: dnsmasqStatus != "active" ? statusCommandStdout : null,
                StatusCommandStderr: dnsmasqStatus != "active" ? statusCommandStderr : null,
                StatusShowOutput: statusShowOutput,
                LogsOutput: logsOutput
            );
            return Ok(status);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Returns the full log file from the path in effective config (log-facility). Untruncated.</summary>
    [HttpGet("logs/download")]
    public async Task<IActionResult> GetLogsDownload(CancellationToken ct)
    {
        var effectiveConfig = _configSetService.GetEffectiveConfig();
        var logsPath = EffectiveDnsmasqConfig.GetLogsPath(effectiveConfig);
        if (string.IsNullOrEmpty(logsPath))
            return NotFound();

        try
        {
            var fullPath = Path.IsPathRooted(logsPath) ? Path.GetFullPath(logsPath) : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), logsPath));
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var content = await System.IO.File.ReadAllTextAsync(fullPath, System.Text.Encoding.UTF8, ct);
            var fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrEmpty(fileName)) fileName = "dnsmasq.log";
            return File(
                System.Text.Encoding.UTF8.GetBytes(content),
                "text/plain",
                fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
