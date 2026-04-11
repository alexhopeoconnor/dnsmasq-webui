using DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;

namespace DnsmasqWebUI.Models.Filters;

public sealed class SplitDnsEditorModel
{
    public SplitDnsRuleMode Mode { get; set; } = SplitDnsRuleMode.Forward;

    /// <summary>Domain path for server=/path/upstream or local=/path/ (labels separated by /).</summary>
    public string DomainPath { get; set; } = "";

    /// <summary>Upstream for server= or rev-server= (may include #port).</summary>
    public string UpstreamServer { get; set; } = "";

    /// <summary>CIDR for rev-server= left side.</summary>
    public string ReverseNetwork { get; set; } = "";

    public void Reset()
    {
        Mode = SplitDnsRuleMode.Forward;
        DomainPath = "";
        UpstreamServer = "";
        ReverseNetwork = "";
    }

    public void LoadFromServerValue(string? raw)
    {
        var p = ServerRuleFields.Parse(raw);
        Mode = SplitDnsRuleMode.Forward;
        DomainPath = p.DomainPath;
        UpstreamServer = p.Target;
        ReverseNetwork = "";
    }

    public void LoadFromRevServerValue(string? raw)
    {
        var p = RevServerRuleFields.Parse(raw);
        Mode = SplitDnsRuleMode.Reverse;
        ReverseNetwork = p.Cidr;
        UpstreamServer = p.Upstream;
        DomainPath = "";
    }

    public void LoadFromLocalValue(string? raw)
    {
        var p = LocalRuleFields.Parse(raw);
        Mode = SplitDnsRuleMode.LocalOnly;
        DomainPath = p.DomainPath;
        UpstreamServer = "";
        ReverseNetwork = "";
    }

    public string? TryBuildConfigLine()
    {
        return Mode switch
        {
            SplitDnsRuleMode.Forward => BuildServerLine(),
            SplitDnsRuleMode.Reverse => BuildRevServerLine(),
            SplitDnsRuleMode.LocalOnly => BuildLocalLine(),
            _ => null
        };
    }

    private string? BuildServerLine()
    {
        var d = DomainPath.Trim();
        var u = UpstreamServer.Trim();
        if (d.Length == 0 || u.Length == 0)
            return null;
        return $"/{d}/{u}";
    }

    private string? BuildRevServerLine()
    {
        var c = ReverseNetwork.Trim();
        var u = UpstreamServer.Trim();
        if (c.Length == 0 || u.Length == 0)
            return null;
        return $"{c},{u}";
    }

    private string? BuildLocalLine()
    {
        var d = DomainPath.Trim();
        if (d.Length == 0)
            return "//";
        return $"/{d}/";
    }
}
