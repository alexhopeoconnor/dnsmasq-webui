using System.Diagnostics;
using DnsmasqWebUI.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly DnsmasqOptions _options;

    public StatusController(IOptions<DnsmasqOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            var dnsmasqStatus = GetDnsmasqServiceStatus();
            return Ok(new
            {
                hostsPath = _options.HostsPath,
                configPath = _options.ConfigPath,
                leasesPath = _options.LeasesPath,
                hostsPathExists = !string.IsNullOrEmpty(_options.HostsPath) && System.IO.File.Exists(_options.HostsPath),
                configPathExists = !string.IsNullOrEmpty(_options.ConfigPath) && System.IO.File.Exists(_options.ConfigPath),
                leasesPathConfigured = !string.IsNullOrEmpty(_options.LeasesPath),
                leasesPathExists = !string.IsNullOrEmpty(_options.LeasesPath) && System.IO.File.Exists(_options.LeasesPath),
                reloadCommandConfigured = !string.IsNullOrWhiteSpace(_options.ReloadCommand),
                statusCommandConfigured = !string.IsNullOrWhiteSpace(_options.StatusCommand),
                dnsmasqStatus
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Runs StatusCommand if configured; returns "active" | "inactive" | "unknown" | "notConfigured".</summary>
    private string GetDnsmasqServiceStatus()
    {
        if (string.IsNullOrWhiteSpace(_options.StatusCommand))
            return "notConfigured";

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = "-c \"" + _options.StatusCommand.Replace("\"", "\\\"") + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit(TimeSpan.FromSeconds(5));
            return process.ExitCode == 0 ? "active" : "inactive";
        }
        catch
        {
            return "unknown";
        }
    }
}
