using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

public enum AddressMatchMode
{
    AllDomains,
    DomainPattern
}

public enum AddressResponseMode
{
    NullSinkhole,
    SpecificIp
}

public sealed record AddressRuleFields(
    AddressMatchMode MatchMode,
    string DomainPath,
    AddressResponseMode ResponseMode,
    string ResponseValue,
    string? RawLineFallback = null)
{
    public bool IsRaw => RawLineFallback != null;

    public static AddressRuleFields Parse(string? raw)
    {
        var s = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(s))
            return new(AddressMatchMode.DomainPattern, "", AddressResponseMode.NullSinkhole, "", null);

        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domains, out var tail, out _))
            return Raw(s);

        if (domains.Length == 0)
            return Raw(s);

        var matchMode = domains.Length == 1 && domains[0] == "#"
            ? AddressMatchMode.AllDomains
            : AddressMatchMode.DomainPattern;

        var domainPath = matchMode == AddressMatchMode.AllDomains
            ? ""
            : string.Join("/", domains);

        var t = tail.Trim();
        AddressResponseMode responseMode;
        string responseValue;
        if (t.Length == 0 || t == "#")
        {
            responseMode = AddressResponseMode.NullSinkhole;
            responseValue = "";
        }
        else
        {
            responseMode = AddressResponseMode.SpecificIp;
            responseValue = t;
        }

        return new(matchMode, domainPath, responseMode, responseValue, null);
    }

    public string ToConfigLine()
    {
        if (IsRaw)
            return (RawLineFallback ?? "").Trim();

        var domain = MatchMode == AddressMatchMode.AllDomains ? "#" : DomainPath.Trim();
        var target = ResponseMode == AddressResponseMode.NullSinkhole ? "#" : ResponseValue.Trim();
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(target))
            return "";

        return $"/{domain}/{target}";
    }

    public string ToSummary() =>
        (MatchMode, ResponseMode) switch
        {
            (AddressMatchMode.AllDomains, AddressResponseMode.NullSinkhole) =>
                "all domains return null/sinkhole",
            (AddressMatchMode.AllDomains, AddressResponseMode.SpecificIp) =>
                $"all domains return {ResponseValue}",
            (AddressMatchMode.DomainPattern, AddressResponseMode.NullSinkhole) =>
                $"queries for /{DomainPath}/ return null/sinkhole",
            _ =>
                $"queries for /{DomainPath}/ return {ResponseValue}"
        };

    private static AddressRuleFields Raw(string raw) =>
        new(AddressMatchMode.DomainPattern, "", AddressResponseMode.NullSinkhole, "", raw);
}
