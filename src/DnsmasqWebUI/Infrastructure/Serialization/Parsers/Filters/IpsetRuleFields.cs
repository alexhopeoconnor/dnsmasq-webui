using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record IpsetRuleFields(string DomainPath, string SetName, string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static IpsetRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new("", "", null);

        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domains, out var tail, out _))
            return Raw(s);

        var setName = tail.Trim();
        if (setName.Length == 0)
            return Raw(s);

        return new(string.Join("/", domains), setName, null);
    }

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();
        var d = DomainPath.Trim();
        var n = SetName.Trim();
        if (d.Length == 0 || n.Length == 0)
            return "";
        return $"/{d}/{n}";
    }

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";
        return $"queries for /{DomainPath.Trim()}/ populate IP set {SetName.Trim()}";
    }

    private static IpsetRuleFields Raw(string line) => new("", "", line);
}
