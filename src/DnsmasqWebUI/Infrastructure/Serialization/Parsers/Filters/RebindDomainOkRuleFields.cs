namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record RebindDomainOkRuleFields(string Domain, string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static RebindDomainOkRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new("", null);
        if (s.Contains(',') || s.Contains('/') || s.Contains(' '))
            return Raw(s);
        return new(s, null);
    }

    public string ToConfigLine() =>
        IsRaw ? (RawLineFallback ?? "").Trim() : Domain.Trim();

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";
        var d = Domain.Trim();
        return d.Length == 0
            ? "(empty)"
            : $"rebind protection is relaxed for {d}";
    }

    private static RebindDomainOkRuleFields Raw(string line) => new("", line);
}
