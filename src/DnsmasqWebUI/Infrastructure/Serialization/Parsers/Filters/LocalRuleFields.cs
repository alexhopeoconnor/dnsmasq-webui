using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record LocalRuleFields(string DomainPath, string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static LocalRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new("", null);

        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domains, out var tail, out _))
            return Raw(s);

        if (tail.Trim().Length != 0)
            return Raw(s);

        return new(string.Join("/", domains), null);
    }

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();
        var d = DomainPath.Trim();
        if (string.IsNullOrWhiteSpace(d))
            return "//";
        return $"/{d}/";
    }

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";
        var d = DomainPath.Trim();
        if (d.Length == 0)
            return "unqualified names are answered locally only";
        return $"queries for /{d}/ are answered locally only";
    }

    private static LocalRuleFields Raw(string line) => new("", line);
}
