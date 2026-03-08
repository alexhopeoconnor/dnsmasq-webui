using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReloadController : ControllerBase
{
    private readonly IReloadService _reloadService;
    private readonly IConfigSetCache _configSetCache;
    private readonly ILogger<ReloadController> _logger;

    public ReloadController(IReloadService reloadService, IConfigSetCache configSetCache, ILogger<ReloadController> logger)
    {
        _reloadService = reloadService;
        _configSetCache = configSetCache;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ReloadResult>> Post(CancellationToken ct)
    {
        try
        {
            var result = await _reloadService.ReloadAsync(ct);
            _logger.LogInformation("Reload requested, success={Success}", result.Success);
            if (result.Success)
                _configSetCache.Invalidate();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reload request failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
