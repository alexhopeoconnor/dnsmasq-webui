using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
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
    private readonly ILogger<HostsController> _logger;

    public HostsController(
        IHostsFileService hostsService,
        IHostsCache hostsCache,
        ILogger<HostsController> logger)
    {
        _hostsService = hostsService;
        _hostsCache = hostsCache;
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

    /// <summary>
    /// Returns non-managed source files. Inactive, missing, and unreadable configured sources are retained
    /// with metadata so clients can explain why they do not produce rows.
    /// </summary>
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

    /// <summary>Returns the complete source inventory, including configured directories and the managed path.</summary>
    [HttpGet("sources")]
    public async Task<ActionResult<IReadOnlyList<HostSource>>> GetSources(CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Get hosts sources");
            return Ok(await _hostsCache.GetSourcesAsync(ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get hosts sources failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Returns unified rows for the Hosts page, preserving source awareness.</summary>
    [HttpGet("unified")]
    public async Task<ActionResult<IReadOnlyList<HostsPageRow>>> GetUnified(CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Get unified hosts rows");
            var rows = await _hostsCache.GetUnifiedRowsAsync(ct);
            return Ok(rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get unified hosts failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
