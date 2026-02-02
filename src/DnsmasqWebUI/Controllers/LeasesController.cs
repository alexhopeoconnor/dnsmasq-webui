using DnsmasqWebUI.Models.Dhcp;
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
    public async Task<ActionResult<LeasesResult>> Get(CancellationToken ct)
    {
        try
        {
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
