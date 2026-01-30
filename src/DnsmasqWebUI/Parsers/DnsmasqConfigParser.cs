using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Parses a dnsmasq config file. Format: one option per line, key=value (same as long option without --), # for comments.
/// See: https://thekelleys.org.uk/dnsmasq/docs/dnsmasq-man.html
/// </summary>
public static class DnsmasqConfigParser
{
    /// <summary>Parse a full config file into a list of config lines (blank, comment, dhcp-host, or other directive).</summary>
    public static IReadOnlyList<ConfigLine> ParseFile(IReadOnlyList<string> lines)
    {
        var result = new List<ConfigLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                result.Add(new ConfigLine { Kind = ConfigLineKind.Blank, LineNumber = lineNumber, RawLine = line });
                continue;
            }

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                result.Add(new ConfigLine { Kind = ConfigLineKind.Comment, LineNumber = lineNumber, RawLine = line });
                continue;
            }

            var content = trimmed;
            if (content.StartsWith("##", StringComparison.Ordinal))
                content = content[2..].TrimStart();
            else if (content.StartsWith("#", StringComparison.Ordinal))
                content = content[1..].TrimStart();

            if (content.StartsWith("dhcp-host=", StringComparison.Ordinal))
            {
                var entry = DhcpHostParser.ParseLine(line, lineNumber);
                if (entry != null)
                    result.Add(new ConfigLine { Kind = ConfigLineKind.DhcpHost, LineNumber = lineNumber, RawLine = line, DhcpHost = entry });
                else
                    result.Add(new ConfigLine { Kind = ConfigLineKind.Other, LineNumber = lineNumber, RawLine = line });
                continue;
            }

            result.Add(new ConfigLine { Kind = ConfigLineKind.Other, LineNumber = lineNumber, RawLine = line });
        }

        return result;
    }

    /// <summary>Turn a config line back into the string to write to the file.</summary>
    public static string ToLine(ConfigLine configLine)
    {
        return configLine.Kind switch
        {
            ConfigLineKind.Blank => configLine.RawLine.Length > 0 ? configLine.RawLine : "",
            ConfigLineKind.Comment => configLine.RawLine,
            ConfigLineKind.Other => configLine.RawLine,
            ConfigLineKind.DhcpHost when configLine.DhcpHost != null => DhcpHostParser.ToLine(configLine.DhcpHost),
            _ => configLine.RawLine
        };
    }
}
