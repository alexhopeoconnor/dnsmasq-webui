using DnsmasqWebUI.Models;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly IDnsmasqConfigService _configService;
    private readonly IReloadService _reloadService;

    public ConfigController(IDnsmasqConfigSetService configSetService, IDnsmasqConfigService configService, IReloadService reloadService)
    {
        _configSetService = configSetService;
        _configService = configService;
        _reloadService = reloadService;
    }

    /// <summary>Returns the config set (main + conf-file/conf-dir) and managed file path. Read-only structure for UI.</summary>
    [HttpGet("set")]
    public async Task<ActionResult<DnsmasqConfigSet>> GetSet(CancellationToken ct)
    {
        try
        {
            var set = await _configSetService.GetConfigSetAsync(ct);
            return Ok(set);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Returns the full content of the managed config file (parsed lines and effective addn-hosts path in file for display).</summary>
    [HttpGet("managed")]
    public async Task<ActionResult<ManagedConfigContent>> GetManaged(CancellationToken ct)
    {
        try
        {
            var content = await _configService.ReadManagedConfigAsync(ct);
            return Ok(content);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Writes the full managed config file, then optionally triggers reload. When SystemHostsPath is configured, the managed file is updated so it contains exactly one addn-hosts line pointing to that path (so dnsmasq loads the editable hosts file); other config files may have additional addn-hosts lines.</summary>
    [HttpPut("managed")]
    public async Task<ActionResult<object>> PutManaged([FromBody] List<DnsmasqConfLine>? lines, CancellationToken ct)
    {
        if (lines == null)
            return BadRequest(new { error = "Body required" });
        try
        {
            await _configService.WriteManagedConfigAsync(lines, ct);
            var reload = await _reloadService.ReloadAsync(ct);
            return Ok(new { saved = true, reload = new { reload.Success, reload.ExitCode, reload.StdErr } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
