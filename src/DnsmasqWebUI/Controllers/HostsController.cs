using DnsmasqWebUI.Models;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HostsController : ControllerBase
{
    private readonly IHostsFileService _hostsService;
    private readonly IReloadService _reloadService;

    public HostsController(IHostsFileService hostsService, IReloadService reloadService)
    {
        _hostsService = hostsService;
        _reloadService = reloadService;
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

    [HttpPut]
    public async Task<ActionResult<object>> Put([FromBody] List<HostEntry>? entries, CancellationToken ct)
    {
        if (entries == null)
            return BadRequest(new { error = "Body required" });
        try
        {
            await _hostsService.WriteAsync(entries, ct);
            var reload = await _reloadService.ReloadAsync(ct);
            return Ok(new { saved = true, reload = new { reload.Success, reload.ExitCode, reload.StdErr } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
