using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Hosts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HostsController : ControllerBase
{
    private readonly IHostsFileService _hostsService;
    private readonly IReloadService _reloadService;
    private readonly IHostsCache _hostsCache;
    private readonly ILogger<HostsController> _logger;

    public HostsController(IHostsFileService hostsService, IReloadService reloadService, IHostsCache hostsCache, ILogger<HostsController> logger)
    {
        _hostsService = hostsService;
        _reloadService = reloadService;
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

    [HttpPut]
    public async Task<ActionResult<SaveWithReloadResult>> Put([FromBody] List<HostEntry>? entries, CancellationToken ct)
    {
        if (entries == null)
            return BadRequest(new { error = "Body required" });
        try
        {
            await _hostsService.WriteAsync(entries, ct);
            var reload = await _reloadService.ReloadAsync(ct);
            _logger.LogInformation("Hosts saved, count={Count}, reload success={Success}", entries.Count, reload.Success);
            return Ok(new SaveWithReloadResult(true, reload));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Hosts put validation failed");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Put hosts failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
