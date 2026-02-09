using DnsmasqWebUI.Infrastructure.Logging;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReloadController : ControllerBase
{
    private readonly IReloadService _reloadService;
    private readonly ILogger<ReloadController> _logger;

    public ReloadController(IReloadService reloadService, ILogger<ReloadController> logger)
    {
        _reloadService = reloadService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ReloadResult>> Post(CancellationToken ct)
    {
        try
        {
            var result = await _reloadService.ReloadAsync(ct);
            _logger.LogInformation(LogEvents.ReloadRequestSuccess, "Reload requested, success={Success}", result.Success);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.ReloadRequestFailed, ex, "Reload request failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
