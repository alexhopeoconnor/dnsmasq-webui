namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record AliasRuleFields(string Left, string Right, string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static AliasRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new("", "", null);

        var idx = s.IndexOf(',');
        if (idx < 0)
            return Raw(s);

        var left = s[..idx].Trim();
        var right = s[(idx + 1)..].Trim();
        if (left.Length == 0 || right.Length == 0)
            return Raw(s);

        return new(left, right, null);
    }

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();
        var a = Left.Trim();
        var b = Right.Trim();
        if (a.Length == 0 || b.Length == 0)
            return "";
        return $"{a},{b}";
    }

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";
        return $"rewrite answers mapping {Left.Trim()} → {Right.Trim()}";
    }

    private static AliasRuleFields Raw(string line) => new("", "", line);
}
