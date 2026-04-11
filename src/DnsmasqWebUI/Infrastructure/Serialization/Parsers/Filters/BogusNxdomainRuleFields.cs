namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public sealed record BogusNxdomainRuleFields(string AddressOrSubnet, string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static BogusNxdomainRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new("", null);
        return new(s, null);
    }

    public string ToConfigLine() =>
        IsRaw ? (RawLineFallback ?? "").Trim() : AddressOrSubnet.Trim();

    public string ToSummary()
    {
        if (IsRaw)
            return RawLineFallback ?? "";
        var a = AddressOrSubnet.Trim();
        return a.Length == 0
            ? "(empty)"
            : $"answers in {a} are treated as bogus NXDOMAIN";
    }

    private static BogusNxdomainRuleFields Raw(string line) => new("", line);
}
