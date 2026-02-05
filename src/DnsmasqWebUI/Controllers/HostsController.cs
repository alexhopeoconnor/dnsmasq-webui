using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HostsController : ControllerBase
{
    private readonly IHostsFileService _hostsService;
    private readonly IReloadService _reloadService;
    private readonly IHostsCache _hostsCache;

    public HostsController(IHostsFileService hostsService, IReloadService reloadService, IHostsCache hostsCache)
    {
        _hostsService = hostsService;
        _reloadService = reloadService;
        _hostsCache = hostsCache;
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
            var snapshot = await _hostsCache.GetSnapshotAsync(ct);
            return Ok(snapshot.ReadOnlyFiles);
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
