using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Leases.Abstractions;
using DnsmasqWebUI.Models.Dhcp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeasesController : ControllerBase
{
    private readonly ILeasesFileService _leasesService;
    private readonly ILeasesCache _cache;
    private readonly ILogger<LeasesController> _logger;

    public LeasesController(ILeasesFileService leasesService, ILeasesCache cache, ILogger<LeasesController> logger)
    {
        _leasesService = leasesService;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<LeasesResult>> Get([FromQuery] bool refresh, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Get leases, refresh={Refresh}", refresh);
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
            _logger.LogError(ex, "Get leases failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
