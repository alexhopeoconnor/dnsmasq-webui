using System.Net;

namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

/// <summary>
/// Parsed <c>--domain=</c> value: <c>domain</c>, <c>domain,range</c>, <c>domain,range,local</c>, or <c>domain,interface</c>.
/// See wwwroot/option-help/domain.html and <c>DomainSemanticHandler</c>.
/// </summary>
public enum DomainRuleScope
{
    /// <summary>Domain only, or <c>domain,local</c> (keyword).</summary>
    Unconditional,
    /// <summary>Second token is IP, CIDR, or similar (not interface name).</summary>
    AddressRangeOrIp,
    /// <summary>Second token is a network interface name.</summary>
    InterfaceName,
}

/// <summary>
/// Editable decomposition of one <c>domain</c> config line. Use <see cref="Parse"/> / <see cref="ToConfigLine"/>.
/// </summary>
public sealed record DomainRuleFields(
    string DomainPart,
    DomainRuleScope Scope,
    string MiddlePart,
    bool AddLocalForRange,
    string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static DomainRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (s.Length == 0)
            return new("", DomainRuleScope.Unconditional, "", false, null);

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length > 3)
            return Raw(s);
        if (tokens.Length == 3 && !string.Equals(tokens[2], "local", StringComparison.OrdinalIgnoreCase))
            return Raw(s);
        if (tokens.Length == 0 || string.IsNullOrEmpty(tokens[0]))
            return Raw(s);

        var domain = tokens[0];
        if (tokens.Length == 1)
            return new(domain, DomainRuleScope.Unconditional, "", false, null);

        var second = tokens[1];
        if (second.Length == 0)
            return Raw(s);

        if (tokens.Length == 2)
        {
            if (string.Equals(second, "local", StringComparison.OrdinalIgnoreCase))
                return new(domain, DomainRuleScope.Unconditional, "", true, null);
            if (IsLikelyInterface(second))
                return new(domain, DomainRuleScope.InterfaceName, second, false, null);
            return new(domain, DomainRuleScope.AddressRangeOrIp, second, false, null);
        }

        return new(domain, DomainRuleScope.AddressRangeOrIp, second, true, null);
    }

    private static bool IsLikelyInterface(string second)
    {
        if (second.Contains('/') || IPAddress.TryParse(second, out _))
            return false;
        return DnsmasqRelaySyntax.IsInterfaceName(second);
    }

    private static DomainRuleFields Raw(string line) =>
        new("", DomainRuleScope.Unconditional, "", false, line);

    /// <summary>Structured editor "raw line" mode; <see cref="ToConfigLine"/> returns the line as-is (after trim).</summary>
    public static DomainRuleFields FromRawConfigLine(string? line) =>
        new("", DomainRuleScope.Unconditional, "", false, (line ?? "").Trim());

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();

        var d = DomainPart.Trim();
        if (d.Length == 0)
            return "";

        return Scope switch
        {
            DomainRuleScope.Unconditional => AddLocalForRange ? $"{d},local" : d,
            DomainRuleScope.InterfaceName =>
                string.IsNullOrWhiteSpace(MiddlePart) ? d : $"{d},{MiddlePart.Trim()}",
            DomainRuleScope.AddressRangeOrIp => BuildRangeLine(d),
            _ => d,
        };
    }

    private string BuildRangeLine(string d)
    {
        var m = MiddlePart.Trim();
        if (m.Length == 0)
            return AddLocalForRange ? $"{d},local" : d;
        return AddLocalForRange ? $"{d},{m},local" : $"{d},{m}";
    }

    /// <summary>Compact description for read-only UI (not the literal config line).</summary>
    public static string FormatSummary(string? configLine)
    {
        var s = (configLine ?? "").Trim();
        if (s.Length == 0)
            return "(empty)";

        var f = Parse(s);
        if (f.IsRaw)
            return s;

        var dom = f.DomainPart == "#" ? "# (resolv.conf search)" : f.DomainPart;
        if (f.Scope == DomainRuleScope.Unconditional)
            return f.AddLocalForRange ? $"{dom} · local DNS" : dom;

        if (f.Scope == DomainRuleScope.InterfaceName)
        {
            var iface = f.MiddlePart.Trim();
            return iface.Length > 0 ? $"{dom} · interface {iface}" : dom;
        }

        var range = f.MiddlePart.Trim();
        if (range.Length == 0)
            return dom;
        return f.AddLocalForRange ? $"{dom} · {range} · local DNS" : $"{dom} · {range}";
    }
}
