using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.EffectiveConfig;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Parses a single line from a dnsmasq .conf file into a <see cref="DnsmasqConfDirective"/> using
/// <see cref="DnsmasqConfOptionRegistry"/> to resolve option kind and dispatching to the
/// appropriate backing model. Add parsers here for each option kind we support.
/// </summary>
public static class DnsmasqConfDirectiveParser
{
    // Key=value line using shared helper (Then/IgnoreThen for hot path). Full line.
    private static readonly TextParser<(string key, string value)> DirectiveLine =
        ConfParserHelpers.KeyValueLine.Named("key=value directive");

    /// <summary>Strip dnsmasq-style comment: from first '#' that is at word start (after whitespace) to end of line.</summary>
    public static string StripComment(string line)
    {
        var result = ConfParserHelpers.StripCommentContent.TryParse(line);
        return result.HasValue ? result.Value : line.TrimEnd();
    }

    /// <summary>Parse a non-comment line into key and value. Returns null for empty or comment-only lines.
    /// Strips dnsmasq-style comments (# and rest when # is after whitespace). Supports key-only lines (value "").</summary>
    public static (string key, string value)? TryParseKeyValue(string line)
    {
        var t = StripComment(line).TrimStart();
        if (string.IsNullOrEmpty(t))
            return null;
        if (t.StartsWith("#", StringComparison.Ordinal))
            return null;
        var result = DirectiveLine.TryParse(t);
        if (!result.HasValue)
            return null;
        var (key, value) = result.Value;
        if (string.IsNullOrWhiteSpace(key))
            return null;
        return (key, value);
    }

    /// <summary>Like <see cref="TryParseKeyValue"/> but when parsing fails returns the Superpower error message and position (line/column).</summary>
    public static bool TryParseKeyValue(string line, out (string key, string value)? kv, out string? error, out Position errorPosition)
    {
        kv = null;
        error = null;
        errorPosition = Position.Empty;
        var t = StripComment(line).TrimStart();
        if (string.IsNullOrEmpty(t) || t.StartsWith("#", StringComparison.Ordinal))
            return true; // not a directive line, no error
        var result = DirectiveLine.TryParse(t);
        if (result.HasValue)
        {
            var (key, value) = result.Value;
            if (string.IsNullOrWhiteSpace(key))
                return true;
            kv = (key, value);
            return true;
        }
        error = result.ToString();
        errorPosition = result.ErrorPosition;
        return false;
    }

    /// <summary>Parse one non-blank, non-comment .conf line into a typed directive. Returns null if line is empty or comment.</summary>
    public static DnsmasqConfDirective? ParseLine(string line, int lineNumber, string sourceFilePath)
    {
        var kv = TryParseKeyValue(line);
        if (kv == null)
            return null;

        var (key, value) = kv.Value;
        var kind = DnsmasqConfOptionRegistry.GetKind(key);
        var dir = Path.GetDirectoryName(sourceFilePath) ?? "";

        static string ResolvePath(string baseDir, string val)
        {
            if (string.IsNullOrEmpty(val)) return val;
            return Path.IsPathRooted(val) ? Path.GetFullPath(val) : Path.GetFullPath(Path.Combine(baseDir, val));
        }

        static ConfDirOption ParseConfDirValue(string value, string baseDir, int lineNumber, string sourceFilePath)
        {
            var parts = value.Split(',', 2);
            var path = parts[0].Trim();
            var suffix = parts.Length > 1 ? parts[1].Trim() : null;
            if (string.IsNullOrEmpty(suffix)) suffix = null;
            return new ConfDirOption(ResolvePath(baseDir, path), suffix, lineNumber, sourceFilePath);
        }

        object typed = kind switch
        {
            DnsmasqOptionKind.ConfFile => new ConfFileOption(ResolvePath(dir, value), lineNumber, sourceFilePath),
            DnsmasqOptionKind.ConfDir => ParseConfDirValue(value, dir, lineNumber, sourceFilePath),
            DnsmasqOptionKind.AddnHosts => new AddnHostsOption(ResolvePath(dir, value), lineNumber, sourceFilePath),
            DnsmasqOptionKind.DhcpLeaseFile => new DhcpLeaseFileOption(ResolvePath(dir, value), lineNumber, sourceFilePath),
            DnsmasqOptionKind.Domain => new DomainOption(value, lineNumber, sourceFilePath),
            DnsmasqOptionKind.DhcpRange => new DhcpRangeOption(value, lineNumber, sourceFilePath),
            DnsmasqOptionKind.DhcpOption => new DhcpOptionOption(value, lineNumber, sourceFilePath),
            DnsmasqOptionKind.DhcpHost => DnsmasqConfDhcpHostLineParser.ParseLine(key + (string.IsNullOrEmpty(value) ? "" : "=" + value), lineNumber) is { } dhcp
                ? dhcp
                : new RawOption(key, value, lineNumber, sourceFilePath),
            DnsmasqOptionKind.Server => new ServerOption(value, lineNumber, sourceFilePath),
            DnsmasqOptionKind.Local => new LocalOption(value, lineNumber, sourceFilePath),
            DnsmasqOptionKind.Address => new AddressOption(value, lineNumber, sourceFilePath),
            DnsmasqOptionKind.String => new RawOption(key, value, lineNumber, sourceFilePath),
            DnsmasqOptionKind.Path => new PathOption(key, ResolvePath(dir, value), lineNumber, sourceFilePath),
            DnsmasqOptionKind.Flag => new RawOption(key, "", lineNumber, sourceFilePath),
            _ => new RawOption(key, value, lineNumber, sourceFilePath)
        };

        return new DnsmasqConfDirective
        {
            LineNumber = lineNumber,
            SourceFilePath = sourceFilePath,
            Kind = kind,
            TypedOption = typed
        };
    }
}
