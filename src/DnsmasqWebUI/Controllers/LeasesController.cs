using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeasesController : ControllerBase
{
    private readonly ILeasesFileService _leasesService;
    private readonly ILeasesCache _cache;

    public LeasesController(ILeasesFileService leasesService, ILeasesCache cache)
    {
        _leasesService = leasesService;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<LeasesResult>> Get([FromQuery] bool refresh, CancellationToken ct)
    {
        try
        {
            if (refresh)
                _cache.Invalidate();
            var (available, entries) = await _leasesService.TryReadAsync(ct);
            if (!available)
                return Ok(new LeasesResult(false, null, "Leases not configured."));
            if (entries == null)
                return Ok(new LeasesResult(true, Array.Empty<LeaseEntry>(), "Leases file not readable."));
            return Ok(new LeasesResult(true, entries, null));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
