using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record ServerRuleFields(
    bool IsScoped,
    string DomainPath,
    string Target,
    string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static ServerRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new(false, "", "", null);

        if (!s.StartsWith("/", StringComparison.Ordinal))
            return new(false, "", s, null);

        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domains, out var tail, out _))
            return Raw(s);

        var path = string.Join("/", domains);
        return new(true, path, tail.Trim(), null);
    }

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();
        if (!IsScoped)
            return Target.Trim();
        var d = DomainPath.Trim();
        var t = Target.Trim();
        if (string.IsNullOrWhiteSpace(d))
            return "";
        return $"/{d}/{t}";
    }

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";

        if (!IsScoped)
        {
            var u = Target.Trim();
            return u == "#"
                ? "upstream forwarding is disabled for this server line"
                : $"use upstream {u}";
        }

        var path = DomainPath.Trim();
        var tgt = Target.Trim();
        if (tgt.Length == 0)
            return $"queries for /{path}/ are not forwarded upstream (local resolution)";
        if (tgt == "#")
            return $"queries for /{path}/ are not forwarded upstream";
        return $"queries for /{path}/ go to {tgt}";
    }

    private static ServerRuleFields Raw(string line) =>
        new(false, "", "", line);
}
