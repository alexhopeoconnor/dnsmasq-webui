using DnsmasqWebUI.Models;
using Sprache;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Parses dnsmasq DHCPv4 lease file lines.
/// </summary>
/// <remarks>
/// Format (authoritative, from dnsmasq author Simon Kelley, dnsmasq-discuss 2006):
/// Five space-separated fields per line:
/// 1) Lease expiry time (seconds since epoch, 1970).
/// 2) MAC address (hex bytes with ':', optionally prefixed by ARP type and '-', e.g. 02:0f:b0:3a:b5:0b).
/// 3) IP address (dotted-quad).
/// 4) Hostname (unqualified, no domain); '*' if unknown.
/// 5) Client-ID (hex bytes with ':'); '*' if not provided.
/// Empty lines return null. When DHCPv6 is in use, a "duid ..." line may appear (first field not numeric);
/// such lines return null. IPv6 lease lines have a different field layout and are not supported here.
/// See: https://lists.thekelleys.org.uk/pipermail/dnsmasq-discuss/2006q2/000734.html
/// </remarks>
public static class LeasesParser
{
    // Non-whitespace token (MAC, IP, hostname, or * for unknown)
    private static readonly Parser<string> Field =
        Parse.AnyChar.Where(c => !char.IsWhiteSpace(c)).AtLeastOnce().Text().Token();

    private static readonly Parser<LeaseEntry> LineParser =
        (from epoch in Parse.Number.Token().Select(s => long.Parse(s))
         from mac in Field
         from address in Field
         from name in Field
         from clientId in Field
         select new LeaseEntry
         {
             Epoch = epoch,
             Mac = mac,
             Address = address,
             Name = name,
             ClientId = clientId
         }).End();

    public static LeaseEntry? ParseLine(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        var result = LineParser.TryParse(trimmed);
        return result.WasSuccessful ? result.Value : null;
    }
}
