using System.Text.RegularExpressions;

namespace DnsmasqWebUI.Infrastructure.Helpers.Dnsmasq;

/// <summary>Parses dnsmasq version from command output (e.g. "dnsmasq --version" stdout/stderr).</summary>
public static class DnsmasqVersionParser
{
    private static readonly Regex Rx = new(@"\b(\d+)\.(\d+)(?:\.(\d+))?\b", RegexOptions.Compiled);

    /// <summary>Finds the first X.Y or X.Y.Z token in combined stdout and stderr; returns null if none found.</summary>
    public static Version? TryParse(string? stdout, string? stderr)
    {
        var text = $"{stdout}\n{stderr}";
        var m = Rx.Match(text);
        if (!m.Success) return null;
        var major = int.Parse(m.Groups[1].Value);
        var minor = int.Parse(m.Groups[2].Value);
        var patch = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
        return new Version(major, minor, patch);
    }
}
