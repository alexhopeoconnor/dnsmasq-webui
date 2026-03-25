using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Models.Hosts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HostsController : ControllerBase
{
    private readonly IHostsFileService _hostsService;
    private readonly IHostsCache _hostsCache;
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly ILogger<HostsController> _logger;

    public HostsController(
        IHostsFileService hostsService,
        IHostsCache hostsCache,
        IDnsmasqConfigSetService configSetService,
        ILogger<HostsController> logger)
    {
        _hostsService = hostsService;
        _hostsCache = hostsCache;
        _configSetService = configSetService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HostEntry>>> Get(CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Get hosts");
            var entries = await _hostsService.ReadAsync(ct);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get hosts failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Returns entries from each read-only hosts file: system hosts (when SystemHostsPath set and no-hosts is false), then addn-hosts that are not the managed file. System hosts is excluded when no-hosts is set because dnsmasq does not read it then.</summary>
    [HttpGet("readonly")]
    public async Task<ActionResult<IReadOnlyList<ReadOnlyHostsFile>>> GetReadOnly(CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Get readonly hosts");
            var snapshot = await _hostsCache.GetSnapshotAsync(ct);
            return Ok(snapshot.ReadOnlyFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get readonly hosts failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Returns unified rows for the Hosts page, preserving source awareness.</summary>
    [HttpGet("unified")]
    public async Task<ActionResult<IReadOnlyList<HostsPageRow>>> GetUnified(
        [FromQuery] bool expandHosts,
        [FromQuery] string? domain,
        [FromQuery] bool noHosts,
        [FromQuery] string? managedHostsPath,
        CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Get unified hosts rows");
            var rows = await _hostsCache.GetUnifiedRowsAsync(expandHosts, domain, noHosts, managedHostsPath, ct);
            return Ok(rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get unified hosts failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
