namespace DnsmasqWebUI.Models;

/// <summary>
/// One parsed directive from a dnsmasq .conf file. Kind tells which option type;
/// TypedOption is the backing model when we have a parser for it (AddnHostsOption, DhcpLeaseFileOption, etc.),
/// otherwise RawOption with Key and Value.
/// </summary>
public sealed class DnsmasqConfDirective
{
    public int LineNumber { get; init; }
    public string SourceFilePath { get; init; } = "";
    public DnsmasqOptionKind Kind { get; init; }
    public object TypedOption { get; init; } = null!; // AddnHostsOption | DhcpLeaseFileOption | DomainOption | RawOption | ...
}
