namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record FilterRrRuleFields(string MatchValue, string RecordType, string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static FilterRrRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new("", "", null);

        var lastComma = s.LastIndexOf(',');
        if (lastComma <= 0 || lastComma >= s.Length - 1)
            return Raw(s);

        var match = s[..lastComma].Trim();
        var rr = s[(lastComma + 1)..].Trim();
        if (match.Length == 0 || rr.Length == 0)
            return Raw(s);

        return new(match, rr, null);
    }

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();
        var m = MatchValue.Trim();
        var r = RecordType.Trim();
        if (m.Length == 0 || r.Length == 0)
            return "";
        return $"{m},{r}";
    }

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";
        return $"responses matching {MatchValue.Trim()} have RR type {RecordType.Trim()} filtered out";
    }

    private static FilterRrRuleFields Raw(string line) => new("", "", line);
}
