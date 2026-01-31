using DnsmasqWebUI.Models;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Parses a dnsmasq .conf file line-by-line into <see cref="DnsmasqConfLine"/> variants (BlankLine, CommentLine, DhcpHostLine, OtherLine).
/// Format: one option per line, key=value (same as long option without --), # for comments.
/// See: https://thekelleys.org.uk/dnsmasq/docs/dnsmasq-man.html
/// </summary>
public static class DnsmasqConfFileLineParser
{
    private enum ConfLineParseKind { Blank, Comment, AddnHosts, DhcpHostCandidate, Other }

    private static readonly TextParser<(ConfLineParseKind kind, string content)> Blank =
        Character.WhiteSpace.Many().AtEnd().Select(_ => (ConfLineParseKind.Blank, ""));

    private static readonly TextParser<(ConfLineParseKind kind, string content)> Comment =
        Character.EqualTo('#').IgnoreThen(Character.AnyChar.Many().Text())
            .Select(_ => (ConfLineParseKind.Comment, ""));

    private static readonly TextParser<(ConfLineParseKind kind, string content)> AddnHosts =
        ConfParserHelpers.OptionalCommentPrefix.IgnoreThen(Span.EqualTo("addn-hosts="))
            .IgnoreThen(Character.AnyChar.Many().Text())
            .Select(s => (ConfLineParseKind.AddnHosts, s.Trim()));

    private static readonly TextParser<(ConfLineParseKind kind, string content)> DhcpHostCandidate =
        ConfParserHelpers.OptionalCommentPrefix.IgnoreThen(Span.EqualTo("dhcp-host="))
            .IgnoreThen(Character.AnyChar.Many())
            .Select(_ => (ConfLineParseKind.DhcpHostCandidate, ""));

    private static readonly TextParser<(ConfLineParseKind kind, string content)> Other =
        Character.AnyChar.Many().Text().Select(s => (ConfLineParseKind.Other, s));

    private static readonly TextParser<(ConfLineParseKind kind, string content)> LineParser =
        Blank.Try().Or(Comment.Try()).Or(AddnHosts.Try()).Or(DhcpHostCandidate.Try()).Or(Other).AtEnd();

    /// <summary>Parse a full config file into a list of config lines (concrete types).</summary>
    public static IReadOnlyList<DnsmasqConfLine> ParseFile(IReadOnlyList<string> lines)
    {
        var result = new List<DnsmasqConfLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                result.Add(new BlankLine { LineNumber = lineNumber, RawLine = line });
                continue;
            }

            var parsed = LineParser.TryParse(trimmed);
            if (!parsed.HasValue)
            {
                result.Add(new OtherLine { LineNumber = lineNumber, RawLine = line });
                continue;
            }

            var (kind, content) = parsed.Value;
            switch (kind)
            {
                case ConfLineParseKind.Blank:
                    result.Add(new BlankLine { LineNumber = lineNumber, RawLine = line });
                    break;
                case ConfLineParseKind.Comment:
                    result.Add(new CommentLine { LineNumber = lineNumber, RawLine = line });
                    break;
                case ConfLineParseKind.AddnHosts:
                    result.Add(new AddnHostsLine { LineNumber = lineNumber, AddnHostsPath = content });
                    break;
                case ConfLineParseKind.DhcpHostCandidate:
                    var dhcp = DnsmasqConfDhcpHostLineParser.ParseLine(line, lineNumber);
                    if (dhcp != null)
                        result.Add(new DhcpHostLine { LineNumber = lineNumber, DhcpHost = dhcp });
                    else
                        result.Add(new OtherLine { LineNumber = lineNumber, RawLine = line });
                    break;
                default:
                    result.Add(new OtherLine { LineNumber = lineNumber, RawLine = line });
                    break;
            }
        }

        return result;
    }

    /// <summary>Turn a config line back into the string to write to the file.</summary>
    public static string ToLine(DnsmasqConfLine line)
    {
        return line switch
        {
            BlankLine b => b.RawLine.Length > 0 ? b.RawLine : "",
            CommentLine c => c.RawLine,
            AddnHostsLine a => "addn-hosts=" + a.AddnHostsPath,
            DhcpHostLine d => DnsmasqConfDhcpHostLineParser.ToLine(d.DhcpHost),
            OtherLine o => o.RawLine,
            _ => ""
        };
    }
}
