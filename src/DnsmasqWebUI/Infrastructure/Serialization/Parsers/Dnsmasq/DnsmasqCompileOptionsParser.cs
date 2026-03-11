using System.Text.RegularExpressions;
using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Dnsmasq;

/// <summary>
/// Parses "Compile time options: ..." line from dnsmasq --version output
/// to determine DHCP, TFTP, DNSSEC, DBus support.
/// </summary>
public static class DnsmasqCompileOptionsParser
{
    private static readonly Regex CompileLine = new(
        @"Compile\s+time\s+options\s*:\s*(?<opts>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public static DnsmasqCompileCapabilities Parse(string? stdout, string? stderr)
    {
        var text = $"{stdout}\n{stderr}";
        var m = CompileLine.Match(text);
        if (!m.Success)
            return new DnsmasqCompileCapabilities(false, false, false, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var tokens = m.Groups["opts"].Value
            .Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool Has(string s) => tokens.Contains(s);

        return new DnsmasqCompileCapabilities(
            Dhcp: Has("DHCP") || Has("DHCPv4") || Has("DHCPv6"),
            Tftp: Has("TFTP"),
            Dnssec: Has("DNSSEC"),
            Dbus: Has("DBus"),
            RawTokens: tokens
        );
    }
}
