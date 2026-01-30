using DnsmasqWebUI.Models;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

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
    // Allow optional whitespace around a parser
    private static TextParser<T> Token<T>(TextParser<T> parser) =>
        Character.WhiteSpace.Many().IgnoreThen(parser).Then(x =>
            Character.WhiteSpace.Many().IgnoreThen(Parse.Return(x)));

    // Non-whitespace token (MAC, IP, hostname, or * for unknown)
    private static readonly TextParser<string> Field =
        Character.Matching(c => !char.IsWhiteSpace(c), "field").AtLeastOnce().Text().Then(s =>
            Character.WhiteSpace.Many().IgnoreThen(Parse.Return(s)));

    private static readonly TextParser<LeaseEntry> LineParser =
        (from epoch in Token(Numerics.IntegerInt64)
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
         }).AtEnd();

    public static LeaseEntry? ParseLine(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        var result = LineParser.TryParse(trimmed);
        return result.HasValue ? result.Value : null;
    }
}
