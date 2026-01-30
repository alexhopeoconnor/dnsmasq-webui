using DnsmasqWebUI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeasesController : ControllerBase
{
    private readonly ILeasesFileService _leasesService;

    public LeasesController(ILeasesFileService leasesService)
    {
        _leasesService = leasesService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> Get(CancellationToken ct)
    {
        try
        {
            var (available, entries) = await _leasesService.TryReadAsync(ct);
            if (!available)
                return Ok(new { available = false, entries = (IReadOnlyList<object>?)null, message = "Leases not configured." });
            if (entries == null)
                return Ok(new { available = true, entries = Array.Empty<object>(), message = "Leases file not readable." });
            return Ok(new { available = true, entries });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
