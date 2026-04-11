namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record RevServerRuleFields(string Cidr, string Upstream, string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static RevServerRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new("", "", null);

        var idx = s.IndexOf(',');
        if (idx < 0)
            return Raw(s);

        var cidr = s[..idx].Trim();
        var upstream = s[(idx + 1)..].Trim();
        if (cidr.Length == 0 || upstream.Length == 0)
            return Raw(s);

        return new(cidr, upstream, null);
    }

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();
        var c = Cidr.Trim();
        var u = Upstream.Trim();
        if (c.Length == 0 || u.Length == 0)
            return "";
        return $"{c},{u}";
    }

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";
        return $"reverse lookups for {Cidr.Trim()} go to {Upstream.Trim()}";
    }

    private static RevServerRuleFields Raw(string line) => new("", "", line);
}
