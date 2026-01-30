using System.Text.RegularExpressions;
using DnsmasqWebUI.Models;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Parses dnsmasq <c>dhcp-host=</c> lines. Config file uses same format as long option without <c>--</c>.
/// Official format: --dhcp-host=[hwaddr][,id:client_id|*][,set:tag][,tag:tag][,ipaddr][,hostname][,lease_time][,ignore]
/// Multiple hwaddr allowed (one IP for several MACs). See: https://thekelleys.org.uk/dnsmasq/docs/dnsmasq-man.html
/// </summary>
public static class DhcpHostParser
{
    private static readonly Regex HostnameRegex = new(@"^[a-zA-Z][-_a-zA-Z0-9]*$", RegexOptions.Compiled);

    // Allow optional whitespace around a parser
    private static TextParser<T> Token<T>(TextParser<T> parser) =>
        Character.WhiteSpace.Many().IgnoreThen(parser).Then(x =>
            Character.WhiteSpace.Many().IgnoreThen(Parse.Return(x)));

    // Optional ## or # at start (Try so that single # backtracks and we can match one #)
    private static readonly TextParser<(bool isComment, bool isDeleted)> Prefix =
        Character.EqualTo('#').Repeat(2).Select(_ => (true, true)).Try()
            .Or(Character.EqualTo('#').Select(_ => (true, false)))
            .OptionalOrDefault((false, false));

    // Literal "dhcp-host=" (consumed, value discarded)
    private static readonly TextParser<Unit> DhcpHostTag =
        Token(Span.EqualTo("dhcp-host=")).Value(Unit.Value);

    // One field: no comma, no # (stops at next comma or trailing comment)
    private static readonly TextParser<string> Field =
        Character.Matching(c => c != ',' && c != '#', "field").AtLeastOnce().Text().Then(s =>
            Character.WhiteSpace.Many().IgnoreThen(Parse.Return(s)));

    // Comma-delimited fields, then optional # comment
    private static readonly TextParser<(List<string> fields, string? comment)> FieldsAndComment =
        from fields in Field.AtLeastOnceDelimitedBy(Token(Character.EqualTo(',')))
        from comment in Token(Character.EqualTo('#')).IgnoreThen(Character.AnyChar.Many().Text())
            .Select(s => (string?)s).OptionalOrDefault(null)
        select (fields.ToList(), string.IsNullOrEmpty(comment) ? null : comment.Trim());

    // Full line: optional ##/# prefix, "dhcp-host=", comma-separated fields, optional # comment
    private static readonly TextParser<(bool isComment, bool isDeleted, List<string> fields, string? comment)> LineParser =
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
        if (!result.HasValue)
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
