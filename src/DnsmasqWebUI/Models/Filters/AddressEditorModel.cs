using DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

namespace DnsmasqWebUI.Models.Filters;

public sealed class AddressEditorModel
{
    public bool RawMode { get; set; }
    public string RawLine { get; set; } = "";

    public AddressMatchMode MatchMode { get; set; } = AddressMatchMode.DomainPattern;
    public string DomainPath { get; set; } = "";
    public AddressResponseMode ResponseMode { get; set; } = AddressResponseMode.NullSinkhole;
    public string ResponseValue { get; set; } = "";

    public static AddressEditorModel FromRaw(string? raw)
    {
        var m = new AddressEditorModel();
        m.LoadFrom(raw);
        return m;
    }

    public void LoadFrom(string? raw)
    {
        var p = AddressRuleFields.Parse(raw);
        if (p.IsRaw)
        {
            RawMode = true;
            RawLine = p.RawLineFallback ?? "";
            MatchMode = AddressMatchMode.DomainPattern;
            DomainPath = "";
            ResponseMode = AddressResponseMode.NullSinkhole;
            ResponseValue = "";
            return;
        }

        RawMode = false;
        RawLine = "";
        MatchMode = p.MatchMode;
        DomainPath = p.DomainPath;
        ResponseMode = p.ResponseMode;
        ResponseValue = p.ResponseValue;
    }

    public AddressRuleFields ToFields()
    {
        if (RawMode)
            return new AddressRuleFields(
                AddressMatchMode.DomainPattern,
                "",
                AddressResponseMode.NullSinkhole,
                "",
                RawLine.Trim());

        return new AddressRuleFields(MatchMode, DomainPath.Trim(), ResponseMode, ResponseValue.Trim(), null);
    }

    public void ResetForAdd()
    {
        RawMode = false;
        RawLine = "";
        MatchMode = AddressMatchMode.DomainPattern;
        DomainPath = "";
        ResponseMode = AddressResponseMode.NullSinkhole;
        ResponseValue = "";
    }
}
