namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record ConnmarkAllowlistRuleFields(string Mark, string Domain, string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static ConnmarkAllowlistRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new("", "", null);

        var idx = s.IndexOf(',');
        if (idx < 0)
            return Raw(s);

        var mark = s[..idx].Trim();
        var domain = s[(idx + 1)..].Trim();
        if (mark.Length == 0 || domain.Length == 0)
            return Raw(s);

        return new(mark, domain, null);
    }

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();
        var m = Mark.Trim();
        var d = Domain.Trim();
        if (m.Length == 0 || d.Length == 0)
            return "";
        return $"{m},{d}";
    }

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";
        return $"mark {Mark.Trim()} is allowed to query {Domain.Trim()}";
    }

    private static ConnmarkAllowlistRuleFields Raw(string line) => new("", "", line);
}
