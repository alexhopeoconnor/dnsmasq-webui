using DnsmasqWebUI.Models;
using DnsmasqWebUI.Options;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HostsController : ControllerBase
{
    private readonly IHostsFileService _hostsService;
    private readonly IReloadService _reloadService;
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly DnsmasqOptions _options;

    public HostsController(
        IHostsFileService hostsService,
        IReloadService reloadService,
        IDnsmasqConfigSetService configSetService,
        Microsoft.Extensions.Options.IOptions<DnsmasqOptions> options)
    {
        _hostsService = hostsService;
        _reloadService = reloadService;
        _configSetService = configSetService;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HostEntry>>> Get(CancellationToken ct)
    {
        try
        {
            var entries = await _hostsService.ReadAsync(ct);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut]
    public async Task<ActionResult<SaveWithReloadResult>> Put([FromBody] List<HostEntry>? entries, CancellationToken ct)
    {
        if (entries == null)
            return BadRequest(new { error = "Body required" });
        var effectiveConfig = _configSetService.GetEffectiveConfig();
        if (string.IsNullOrWhiteSpace(_options.SystemHostsPath))
            return BadRequest(new { error = "No system hosts file configured. Set Dnsmasq:SystemHostsPath to enable hosts editing." });
        // When no-hosts is set, dnsmasq only uses addn-hosts files; allow editing only if our path is in that list.
        var systemPath = Path.GetFullPath(_options.SystemHostsPath.Trim());
        var pathInAddnHosts = effectiveConfig.AddnHostsPaths?.Any(p => string.Equals(p, systemPath, StringComparison.Ordinal)) == true;
        if (effectiveConfig.NoHosts && !pathInAddnHosts)
            return BadRequest(new { error = "Hosts are disabled by no-hosts in dnsmasq config, and the configured path is not in addn-hosts, so dnsmasq would not read it." });
        try
        {
            await _hostsService.WriteAsync(entries, ct);
            var reload = await _reloadService.ReloadAsync(ct);
            return Ok(new SaveWithReloadResult(true, reload));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
