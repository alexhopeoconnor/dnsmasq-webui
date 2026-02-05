using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReloadController : ControllerBase
{
    private readonly IReloadService _reloadService;

    public ReloadController(IReloadService reloadService)
    {
        _reloadService = reloadService;
    }

    [HttpPost]
    public async Task<ActionResult<ReloadResult>> Post(CancellationToken ct)
    {
        try
        {
            var result = await _reloadService.ReloadAsync(ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
