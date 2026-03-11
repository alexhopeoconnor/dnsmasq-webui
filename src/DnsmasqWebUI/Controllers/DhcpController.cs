using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Models.Dhcp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DhcpController : ControllerBase
{
    private readonly IDnsmasqConfigService _configService;
    private readonly ILogger<DhcpController> _logger;

    public DhcpController(IDnsmasqConfigService configService, ILogger<DhcpController> logger)
    {
        _configService = configService;
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
}
