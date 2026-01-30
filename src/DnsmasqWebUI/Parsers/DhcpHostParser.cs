using System.Text.RegularExpressions;
using DnsmasqWebUI.Models;
using Sprache;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Parses dnsmasq <c>dhcp-host=</c> lines. Config file uses same format as long option without <c>--</c>.
/// Official format: --dhcp-host=[hwaddr][,id:client_id|*][,set:tag][,tag:tag][,ipaddr][,hostname][,lease_time][,ignore]
/// Multiple hwaddr allowed (one IP for several MACs). See: https://thekelleys.org.uk/dnsmasq/docs/dnsmasq-man.html
/// </summary>
public static class DhcpHostParser
{
    private static readonly Regex HostnameRegex = new(@"^[a-zA-Z][-_a-zA-Z0-9]*$", RegexOptions.Compiled);

    // Optional ## or # at start
    private static readonly Parser<(bool isComment, bool isDeleted)> Prefix =
        Parse.Char('#').Repeat(1, 2).Optional().Select(o =>
        {
            if (!o.IsDefined) return (false, false);
            var s = string.Concat(o.Get());
            return (true, s.Length == 2);
        });

    // Literal "dhcp-host=" (consumed, value discarded)
    private static readonly Parser<IEnumerable<char>> DhcpHostTag =
        Parse.String("dhcp-host=").Token();

    // One field: no comma, no # (stops at next comma or trailing comment)
    private static readonly Parser<string> Field =
        Parse.AnyChar.Where(c => c != ',' && c != '#').AtLeastOnce().Text().Token();

    // Comma-delimited fields (Sprache DelimitedBy), then optional trailing # comment
    private static readonly Parser<(List<string> fields, string? comment)> FieldsAndComment =
        from fields in Field.DelimitedBy(Parse.Char(',').Token()).Select(l => l.ToList())
        from comment in Parse.Char('#').Token().Then(_ => Parse.AnyChar.Many().Text()).Optional()
        select (fields, comment.IsDefined ? comment.Get().Trim() : null);

    // Full line: optional ##/# prefix, "dhcp-host=", comma-separated fields, optional # comment
    private static readonly Parser<(bool isComment, bool isDeleted, List<string> fields, string? comment)> LineParser =
        from prefix in Prefix
        from _ in DhcpHostTag
        from fc in FieldsAndComment
        select (prefix.isComment, prefix.isDeleted, fc.fields, fc.comment);

    public static DhcpHostEntry? ParseLine(string line, int lineNumber)
    {
        var remain = line.Trim();
        if (!remain.StartsWith("dhcp-host=", StringComparison.Ordinal) &&
            !remain.StartsWith("#dhcp-host=", StringComparison.Ordinal) &&
            !remain.StartsWith("##dhcp-host=", StringComparison.Ordinal))
            return null;

        var result = LineParser.TryParse(remain);
        if (!result.WasSuccessful)
            return null;

        var (isComment, isDeleted, fields, comment) = result.Value;
        var host = new DhcpHostEntry
        {
            LineNumber = lineNumber,
            RawLine = line,
            IsComment = isComment,
            IsDeleted = isDeleted,
            Comment = comment ?? ""
        };
        var macList = new List<string>();
        var extraList = new List<string>();

        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field)) continue;
            if (IsMac(field))
                macList.Add(field);
            else if (IsIpv4(field))
                host.Address = field;
            else if (field.Equals("infinite", StringComparison.OrdinalIgnoreCase))
                host.Lease = field;
            else if (field.Equals("ignore", StringComparison.OrdinalIgnoreCase))
                host.Ignore = true;
            else if (field.Length > 0 && char.IsDigit(field[0]))
                host.Lease = field;
            else if (field.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
                extraList.Add(field);
            else if (field.StartsWith("set:", StringComparison.OrdinalIgnoreCase))
                extraList.Add(field);
            else if (HostnameRegex.IsMatch(field))
                host.Name = field;
            else
                extraList.Add(field);
        }

        host.MacAddresses = macList;
        host.Extra = extraList;
        return host;
    }

    public static string ToLine(DhcpHostEntry h)
    {
        var prefix = h.IsDeleted ? "##" : (h.IsComment || !HasFields(h) ? "#" : "");
        var parts = new List<string>();
        if (h.Extra.Any(e => e.StartsWith("id:", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var e in h.Extra.Where(e => e.StartsWith("id:", StringComparison.OrdinalIgnoreCase)))
                parts.Add(e);
        }
        if (h.MacAddresses.Count > 0)
            parts.Add(string.Join(",", h.MacAddresses));
        if (!string.IsNullOrEmpty(h.Name))
            parts.Add(h.Name);
        if (!string.IsNullOrEmpty(h.Address))
            parts.Add(h.Address);
        var otherExtra = h.Extra.Where(e => !e.StartsWith("id:", StringComparison.OrdinalIgnoreCase)).ToList();
        if (otherExtra.Count > 0)
            parts.Add(string.Join(", ", otherExtra));
        if (!string.IsNullOrEmpty(h.Lease))
            parts.Add(h.Lease);
        if (h.Ignore)
            parts.Add("ignore");

        var line = prefix + "dhcp-host=" + string.Join(", ", parts);
        if (!string.IsNullOrEmpty(h.Comment))
            line += " # " + h.Comment;
        return line;
    }

    private static bool HasFields(DhcpHostEntry h) =>
        h.MacAddresses.Count > 0 || !string.IsNullOrEmpty(h.Name) || !string.IsNullOrEmpty(h.Address) ||
        h.Extra.Count > 0 || !string.IsNullOrEmpty(h.Lease) || h.Ignore;

    private static bool IsMac(string s)
    {
        var parts = s.Split(':');
        return parts.Length == 6 && parts.All(p => p.Length == 2 && p.All(c => char.IsAsciiHexDigit(c)));
    }

    private static bool IsIpv4(string s)
    {
        var parts = s.Split('.');
        return parts.Length == 4 && parts.All(p => p.Length > 0 && p.All(char.IsDigit) && int.TryParse(p, out var n) && n >= 0 && n <= 255);
    }
}
