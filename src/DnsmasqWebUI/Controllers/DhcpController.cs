using DnsmasqWebUI.Models;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DhcpController : ControllerBase
{
    private readonly IDnsmasqConfigService _configService;
    private readonly IReloadService _reloadService;

    public DhcpController(IDnsmasqConfigService configService, IReloadService reloadService)
    {
        _configService = configService;
        _reloadService = reloadService;
    }

    [HttpGet("hosts")]
    public async Task<ActionResult<IReadOnlyList<DhcpHostEntry>>> GetHosts(CancellationToken ct)
    {
        try
        {
            var entries = await _configService.ReadDhcpHostsAsync(ct);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("hosts")]
    public async Task<ActionResult<SaveWithReloadResult>> PutHosts([FromBody] List<DhcpHostEntry>? entries, CancellationToken ct)
    {
        if (entries == null)
            return BadRequest(new { error = "Body required" });
        try
        {
            await _configService.WriteDhcpHostsAsync(entries, ct);
            var reload = await _reloadService.ReloadAsync(ct);
            return Ok(new SaveWithReloadResult(true, reload));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
