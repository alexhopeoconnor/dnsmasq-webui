using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record NftsetRuleFields(string DomainPath, string SetSpecs, string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static NftsetRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new("", "", null);

        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domains, out var tail, out _))
            return Raw(s);

        var specs = tail.Trim();
        if (specs.Length == 0)
            return Raw(s);

        return new(string.Join("/", domains), specs, null);
    }

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();
        var d = DomainPath.Trim();
        var sp = SetSpecs.Trim();
        if (d.Length == 0 || sp.Length == 0)
            return "";
        return $"/{d}/{sp}";
    }

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";
        return $"queries for /{DomainPath.Trim()}/ populate nft set(s): {SetSpecs.Trim()}";
    }

    private static NftsetRuleFields Raw(string line) => new("", "", line);
}
