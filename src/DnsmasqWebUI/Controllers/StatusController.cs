using System.Diagnostics;
using DnsmasqWebUI.Options;
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

    public StatusController(IOptions<DnsmasqOptions> options, IDnsmasqConfigSetService configSetService)
    {
        _options = options.Value;
        _configSetService = configSetService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            var set = await _configSetService.GetConfigSetAsync(ct);
            var (dnsmasqStatus, statusCommandExitCode, statusCommandStdout, statusCommandStderr) = GetDnsmasqServiceStatus(_options.StatusCommand);
            var effectiveLeasesPath = _configSetService.GetLeasesPath();
            var payload = new Dictionary<string, object?>
            {
                ["hostsPath"] = _options.HostsPath,
                ["mainConfigPath"] = _options.MainConfigPath,
                ["managedFilePath"] = set.ManagedFilePath,
                ["leasesPath"] = effectiveLeasesPath,
                ["hostsPathExists"] = !string.IsNullOrEmpty(_options.HostsPath) && System.IO.File.Exists(_options.HostsPath),
                ["mainConfigPathExists"] = !string.IsNullOrEmpty(_options.MainConfigPath) && System.IO.File.Exists(_options.MainConfigPath),
                ["managedFilePathExists"] = !string.IsNullOrEmpty(set.ManagedFilePath) && System.IO.File.Exists(set.ManagedFilePath),
                ["leasesPathConfigured"] = !string.IsNullOrEmpty(effectiveLeasesPath),
                ["leasesPathExists"] = !string.IsNullOrEmpty(effectiveLeasesPath) && System.IO.File.Exists(effectiveLeasesPath),
                ["reloadCommandConfigured"] = !string.IsNullOrWhiteSpace(_options.ReloadCommand),
                ["statusCommandConfigured"] = !string.IsNullOrWhiteSpace(_options.StatusCommand),
                ["dnsmasqStatus"] = dnsmasqStatus
            };
            if (dnsmasqStatus != "active" && statusCommandExitCode.HasValue)
                payload["statusCommandExitCode"] = statusCommandExitCode.Value;
            if (dnsmasqStatus != "active" && !string.IsNullOrEmpty(statusCommandStdout))
                payload["statusCommandStdout"] = statusCommandStdout;
            if (dnsmasqStatus != "active" && !string.IsNullOrEmpty(statusCommandStderr))
                payload["statusCommandStderr"] = statusCommandStderr;
            return Ok(payload);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Runs StatusCommand if configured; returns status and optional exit code/stdout/stderr for UI.</summary>
    private static (string Status, int? ExitCode, string? Stdout, string? Stderr) GetDnsmasqServiceStatus(string? statusCommand)
    {
        if (string.IsNullOrWhiteSpace(statusCommand))
            return ("notConfigured", null, null, null);

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = "-c \"" + statusCommand.Replace("\"", "\\\"") + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(TimeSpan.FromSeconds(5));
            var exitCode = process.HasExited ? process.ExitCode : -1;
            var status = exitCode == 0 ? "active" : "inactive";
            return (status, exitCode,
                string.IsNullOrWhiteSpace(stdout) ? null : stdout.Trim(),
                string.IsNullOrWhiteSpace(stderr) ? null : stderr.Trim());
        }
        catch (Exception ex)
        {
            return ("unknown", null, null, ex.Message);
        }
    }
}
