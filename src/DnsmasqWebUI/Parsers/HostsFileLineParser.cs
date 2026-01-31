using DnsmasqWebUI.Models;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Parses a single line of an /etc/hosts-style file. Format: IP_address canonical_hostname [aliases...]
/// Fields separated by blanks/tabs; text from '#' to EOL is comment (hosts(5), RFC 952).
/// </summary>
public static class HostsFileLineParser
{
    // Token: non-whitespace, non-# (stops at inline comment)
    private static readonly TextParser<string> TokenField =
        Character.Matching(c => !char.IsWhiteSpace(c) && c != '#', "token")
            .AtLeastOnce().Text();

    // Optional leading # (comment line); when present, rest of line is comment text, not data
    private static readonly TextParser<bool> OptionalComment =
        ConfParserHelpers.Token(Character.EqualTo('#')).Select(_ => true).OptionalOrDefault(false);

    // Content: address (first token) then one or more names (tokens). Per hosts(5): IP then canonical name [aliases...].
    private static readonly TextParser<(string address, List<string> names)> Content =
        from address in TokenField
        from _ in Span.WhiteSpace
        from names in TokenField.AtLeastOnce()
        select (address, names.ToList());

    // Full line: if starts with #, treat entire line as comment (do not parse rest as address/names); else parse Content.
    private static readonly TextParser<(bool isComment, string address, List<string> names)> LineContent =
        OptionalComment.Then(hasComment =>
            hasComment
                ? Character.AnyChar.Many().Text().Select(_ => (true, "", new List<string>()))
                : Content.Select(c => (false, c.address, c.names)));

    public static HostEntry? ParseLine(string line, int lineNumber)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return new HostEntry { LineNumber = lineNumber, RawLine = line, IsPassthrough = true };

        var result = LineContent.TryParse(trimmed);
        if (!result.HasValue)
            return new HostEntry { LineNumber = lineNumber, RawLine = line, IsPassthrough = true };

        var (isComment, address, names) = result.Value;
        if (string.IsNullOrEmpty(address) && names.Count == 0)
            return new HostEntry { LineNumber = lineNumber, RawLine = line, IsComment = isComment, IsPassthrough = true };

        return new HostEntry
        {
            LineNumber = lineNumber,
            Address = address,
            Names = names,
            RawLine = line,
            IsComment = isComment
        };
    }

    public static string ToLine(HostEntry entry)
    {
        if (entry.IsPassthrough)
            return entry.RawLine;
        var prefix = entry.IsComment ? "# " : "";
        return prefix + entry.Address + " " + string.Join(" ", entry.Names);
    }
}
