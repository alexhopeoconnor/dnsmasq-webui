using System.Text.Json;
using System.Text.Json.Nodes;
using DnsmasqWebUI.Infrastructure.Logging;
using DnsmasqWebUI.Models.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoggingController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationRoot _configurationRoot;
    private readonly RuntimeOverridesOptions _overridesOptions;
    private readonly ILogger<LoggingController> _logger;

    public LoggingController(
        IConfiguration configuration,
        IOptions<RuntimeOverridesOptions> overridesOptions,
        ILogger<LoggingController> logger)
    {
        _configuration = configuration;
        _configurationRoot = (IConfigurationRoot)configuration;
        _overridesOptions = overridesOptions.Value;
        _logger = logger;
    }

    /// <summary>Get the current minimum log level from configuration (Logging:LogLevel:Default).</summary>
    [HttpGet("level")]
    public ActionResult<LogLevelResponse> GetLevel()
    {
        var level = ParseLevel(_configuration["Logging:LogLevel:Default"]) ?? LogLevel.Information;
        return Ok(new LogLevelResponse(LevelName(level)));
    }

    /// <summary>Set the minimum log level. Writes to appsettings.Overrides.json and reloads config.</summary>
    [HttpPost("level")]
    public ActionResult<LogLevelResponse> SetLevel([FromBody] LogLevelRequest request)
    {
        var level = ParseLevel(request?.LogLevel);
        if (level == null)
        {
            _logger.LogWarning("Set log level failed: invalid level requested");
            return BadRequest(new { error = "Invalid LogLevel. Use Trace, Debug, Information, Warning, Error, or Critical." });
        }

        var path = GetOverridesPath();
        if (!TryUpdateOverrides(path, logLevel: level.Value, excludedPrefixes: null))
        {
            _logger.LogError("Failed to write log level overrides file to {Path}", path);
            return StatusCode(500, new { error = "Failed to write overrides file." });
        }

        _configurationRoot.Reload();
        _logger.LogInformation("Log level changed to {Level}", LevelName(level.Value));
        return Ok(new LogLevelResponse(LevelName(level.Value)));
    }

    /// <summary>Get excluded category prefixes. Prefers runtime overrides (ExcludedCategoryPrefixesOverrides) when present; otherwise uses defaults (ExcludedCategoryPrefixes).</summary>
    [HttpGet("filters")]
    public ActionResult<FiltersResponse> GetFilters()
    {
        var prefixes = AppLogsConfigHelper.GetEffectiveExcludedPrefixes(_configuration);
        return Ok(new FiltersResponse(prefixes));
    }

    /// <summary>Set excluded category prefixes. Writes to appsettings.Overrides.json and reloads config.</summary>
    [HttpPost("filters")]
    public ActionResult<FiltersResponse> SetFilters([FromBody] FiltersRequest request)
    {
        var prefixes = request?.ExcludedCategoryPrefixes ?? [];
        var path = GetOverridesPath();
        if (!TryUpdateOverrides(path, logLevel: null, excludedPrefixes: prefixes))
        {
            _logger.LogError("Failed to write filters overrides file to {Path}", path);
            return StatusCode(500, new { error = "Failed to write overrides file." });
        }

        _configurationRoot.Reload();
        _logger.LogInformation("App logs filters updated, count={Count}", prefixes.Count);
        return Ok(new FiltersResponse(prefixes));
    }

    /// <summary>Restore filter defaults by removing overrides. Uses ExcludedCategoryPrefixes from appsettings.json.</summary>
    [HttpPost("filters/restore-defaults")]
    public ActionResult<FiltersResponse> RestoreFilterDefaults()
    {
        var path = GetOverridesPath();
        if (!TryRemoveFilterOverrides(path))
        {
            _logger.LogError("Failed to remove filter overrides from {Path}", path);
            return StatusCode(500, new { error = "Failed to update overrides file." });
        }

        _configurationRoot.Reload();
        var defaults = AppLogsConfigHelper.GetDefaultExcludedPrefixes(_configuration);
        _logger.LogInformation("App logs filters restored to defaults, count={Count}", defaults.Count);
        return Ok(new FiltersResponse(defaults));
    }

    private string GetOverridesPath()
    {
        var path = _overridesOptions.FilePath?.Trim();
        return string.IsNullOrEmpty(path)
            ? Path.Combine(AppContext.BaseDirectory, RuntimeOverridesOptions.DefaultFileName)
            : path;
    }

    private bool TryUpdateOverrides(string filePath, LogLevel? logLevel, List<string>? excludedPrefixes)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var root = LoadOverrides(filePath);

            if (logLevel.HasValue)
            {
                root["Logging"] = new JsonObject
                {
                    ["LogLevel"] = new JsonObject { ["Default"] = LevelName(logLevel.Value) }
                };
            }

            if (excludedPrefixes != null)
            {
                // Use sentinel when empty so .NET config returns non-null (empty arrays bind to null)
                var values = excludedPrefixes.Count == 0
                    ? new[] { JsonValue.Create(AppLogsConfigHelper.ExplicitlyEmptySentinel) }
                    : excludedPrefixes.Select(s => JsonValue.Create(s)).ToArray();
                root["AppLogs"] = new JsonObject
                {
                    ["ExcludedCategoryPrefixesOverrides"] = new JsonArray(values)
                };
            }

            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryRemoveFilterOverrides(string filePath)
    {
        try
        {
            var root = LoadOverrides(filePath);
            if (root["AppLogs"] is JsonObject appLogs)
            {
                appLogs.Remove("ExcludedCategoryPrefixesOverrides");
                if (appLogs.Count == 0)
                    root.Remove("AppLogs");
            }

            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static JsonObject LoadOverrides(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
            return new JsonObject();

        try
        {
            var json = System.IO.File.ReadAllText(filePath);
            var node = JsonNode.Parse(json);
            return node as JsonObject ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private static string LevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "Trace",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Information",
        LogLevel.Warning => "Warning",
        LogLevel.Error => "Error",
        LogLevel.Critical => "Critical",
        _ => "Information"
    };

    private static LogLevel? ParseLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim() switch
        {
            "Trace" => LogLevel.Trace,
            "Debug" => LogLevel.Debug,
            "Information" or "Info" => LogLevel.Information,
            "Warning" or "Warn" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Critical,
            _ => null
        };
    }

    public record LogLevelRequest(string? LogLevel);
    public record LogLevelResponse(string LogLevel);
    public record FiltersRequest(List<string>? ExcludedCategoryPrefixes);
    public record FiltersResponse(List<string> ExcludedCategoryPrefixes);
}
