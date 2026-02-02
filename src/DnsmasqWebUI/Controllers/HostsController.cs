using System.Text;
using DnsmasqWebUI.Configuration;
using DnsmasqWebUI.Models;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HostsController : ControllerBase
{
    private readonly IHostsFileService _hostsService;
    private readonly IReloadService _reloadService;
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly DnsmasqOptions _options;

    public HostsController(IHostsFileService hostsService, IReloadService reloadService, IDnsmasqConfigSetService configSetService, IOptions<DnsmasqOptions> options)
    {
        _hostsService = hostsService;
        _reloadService = reloadService;
        _configSetService = configSetService;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HostEntry>>> Get(CancellationToken ct)
    {
        try
        {
            var entries = await _hostsService.ReadAsync(ct);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Returns entries from each read-only hosts file: system hosts (when SystemHostsPath set and no-hosts is false), then addn-hosts that are not the managed file. System hosts is excluded when no-hosts is set because dnsmasq does not read it then.</summary>
    [HttpGet("readonly")]
    public async Task<ActionResult<IReadOnlyList<ReadOnlyHostsFile>>> GetReadOnly(CancellationToken ct)
    {
        try
        {
            var set = await _configSetService.GetConfigSetAsync(ct);
            var effectiveConfig = _configSetService.GetEffectiveConfig();
            var addnPaths = effectiveConfig.AddnHostsPaths ?? Array.Empty<string>();
            var managedPath = set.ManagedHostsFilePath != null ? Path.GetFullPath(set.ManagedHostsFilePath) : null;

            var result = new List<ReadOnlyHostsFile>();

            // System hosts: only when configured and no-hosts is false (dnsmasq reads it then).
            var systemPath = _options.SystemHostsPath?.Trim();
            if (!string.IsNullOrEmpty(systemPath) && !effectiveConfig.NoHosts)
            {
                var fullPath = Path.GetFullPath(systemPath);
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        var lines = await System.IO.File.ReadAllLinesAsync(fullPath, Encoding.UTF8, ct);
                        var entries = new List<HostEntry>();
                        for (var i = 0; i < lines.Length; i++)
                        {
                            var entry = HostsFileLineParser.ParseLine(lines[i], i + 1);
                            if (entry != null)
                                entries.Add(entry);
                        }
                        result.Add(new ReadOnlyHostsFile(fullPath, entries));
                    }
                    catch
                    {
                        // Skip unreadable
                    }
                }
            }

            var systemPathFull = !string.IsNullOrEmpty(systemPath) ? Path.GetFullPath(systemPath) : null;

            foreach (var p in addnPaths)
            {
                var fullPath = Path.GetFullPath(p);
                if (managedPath != null && string.Equals(fullPath, managedPath, StringComparison.Ordinal))
                    continue;
                if (systemPathFull != null && string.Equals(fullPath, systemPathFull, StringComparison.Ordinal))
                    continue;
                if (!System.IO.File.Exists(fullPath))
                    continue;
                try
                {
                    var lines = await System.IO.File.ReadAllLinesAsync(fullPath, Encoding.UTF8, ct);
                    var entries = new List<HostEntry>();
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var entry = HostsFileLineParser.ParseLine(lines[i], i + 1);
                        if (entry != null)
                            entries.Add(entry);
                    }
                    result.Add(new ReadOnlyHostsFile(fullPath, entries));
                }
                catch
                {
                    // Skip unreadable files
                }
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut]
    public async Task<ActionResult<SaveWithReloadResult>> Put([FromBody] List<HostEntry>? entries, CancellationToken ct)
    {
        if (entries == null)
            return BadRequest(new { error = "Body required" });
        try
        {
            await _hostsService.WriteAsync(entries, ct);
            var reload = await _reloadService.ReloadAsync(ct);
            return Ok(new SaveWithReloadResult(true, reload));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
