using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(IDnsmasqConfigSetService configSetService, ILogger<ConfigController> logger)
    {
        _configSetService = configSetService;
        _logger = logger;
    }

    /// <summary>Returns the config set (main + conf-file/conf-dir) and managed file path. Read-only structure for UI.</summary>
    [HttpGet("set")]
    public async Task<ActionResult<DnsmasqConfigSet>> GetSet(CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Get config set");
            var set = await _configSetService.GetConfigSetAsync(ct);
            return Ok(set);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get config set failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retired raw managed-config endpoint. Managed configuration is intentionally available only
    /// through the effective-config save transaction, which provides validation, backup, reload,
    /// rollback, and draft-conflict checks.
    /// </summary>
    [HttpGet("managed")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult GetManaged() => RetiredManagedConfigEndpoint();

    /// <summary>
    /// Retired raw managed-config endpoint. Managed configuration is intentionally available only
    /// through the effective-config save transaction, which provides validation, backup, reload,
    /// rollback, and draft-conflict checks.
    /// </summary>
    [HttpPut("managed")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult PutManaged() => RetiredManagedConfigEndpoint();

    private ActionResult RetiredManagedConfigEndpoint()
    {
        const string detail = "Raw managed-config reads and writes are retired. Use the Effective Config and Hosts workflows, which save through the validated backup-and-rollback transaction.";
        _logger.LogWarning("Rejected request to retired raw managed-config endpoint");
        return Problem(
            statusCode: StatusCodes.Status410Gone,
            title: "Raw managed-config endpoint retired",
            detail: detail);
    }
}
