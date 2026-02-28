using System.Reflection;
using System.Text.Json;
using DnsmasqWebUI.Extensions;
using DnsmasqWebUI.Infrastructure.Services.Updates.Abstractions;
using DnsmasqWebUI.Models.Config;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Updates;

public sealed class UpdateCheckService : IUpdateCheckService
{
    private const string ReleasesBaseUrl = "https://github.com/alexhopeoconnor/dnsmasq-webui/releases";
    private static readonly string CurrentVersion = typeof(UpdateCheckService).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<UpdateCheckOptions> _options;

    private readonly object _lock = new();
    private string? _newerVersionTag;
    private string? _newerVersionUrl;
    private DateTime? _lastCheckTime;
    private bool _checkInProgress;

    public UpdateCheckService(IHttpClientFactory httpClientFactory, IOptions<UpdateCheckOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public event EventHandler? ResultChanged;

    public bool NewerVersionAvailable => _newerVersionTag != null;
    public string? NewerVersionTag => _newerVersionTag;
    public string? NewerVersionUrl => _newerVersionUrl;
    public DateTime? LastCheckTime => _lastCheckTime;
    public bool CheckInProgress => _checkInProgress;

    public async Task CheckNowAsync()
    {
        lock (_lock)
        {
            if (_checkInProgress) return;
            _checkInProgress = true;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(ServiceCollectionExtensions.GitHubClientName);
            var response = await client.GetAsync("repos/alexhopeoconnor/dnsmasq-webui/releases/latest");
            string? newTag = null;
            string? newUrl = null;
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
                var htmlUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;
                if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(htmlUrl))
                {
                    var latest = tagName.TrimStart('v');
                    var current = BaseVersion(CurrentVersion);
                    if (!string.IsNullOrEmpty(current) && current != "?" && IsNewerVersion(latest, current))
                    {
                        newTag = tagName.StartsWith('v') ? tagName : "v" + tagName;
                        newUrl = htmlUrl;
                    }
                }
            }

            lock (_lock)
            {
                _newerVersionTag = newTag;
                _newerVersionUrl = newUrl;
                _lastCheckTime = DateTime.UtcNow;
                _checkInProgress = false;
            }

            ResultChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            lock (_lock)
            {
                _lastCheckTime = DateTime.UtcNow;
                _checkInProgress = false;
            }
            ResultChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string BaseVersion(string v)
    {
        if (string.IsNullOrEmpty(v) || v == "?") return v;
        var i = v.IndexOf('+');
        return i >= 0 ? v[..i] : v;
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        var l = ParseVersionParts(latest);
        var c = ParseVersionParts(current);
        for (var i = 0; i < Math.Max(l.Length, c.Length); i++)
        {
            var a = i < l.Length ? l[i] : 0;
            var b = i < c.Length ? c[i] : 0;
            if (a > b) return true;
            if (a < b) return false;
        }
        return false;
    }

    private static int[] ParseVersionParts(string v)
    {
        return v.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
            .ToArray();
    }
}
