using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DhcpController : ControllerBase
{
    private readonly IDnsmasqConfigService _configService;
    private readonly IReloadService _reloadService;
    private readonly ILogger<DhcpController> _logger;

    public DhcpController(IDnsmasqConfigService configService, IReloadService reloadService, ILogger<DhcpController> logger)
    {
        _configService = configService;
        _reloadService = reloadService;
        _logger = logger;
    }

    [HttpGet("hosts")]
    public async Task<ActionResult<IReadOnlyList<DhcpHostEntry>>> GetHosts(CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Get DHCP hosts");
            var entries = await _configService.ReadDhcpHostsAsync(ct);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get DHCP hosts failed");
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
            _logger.LogInformation("DHCP hosts saved, count={Count}, reload success={Success}", entries.Count, reload.Success);
            return Ok(new SaveWithReloadResult(true, reload));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "DHCP hosts put validation failed");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Put DHCP hosts failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
