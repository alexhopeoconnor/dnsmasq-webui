namespace DnsmasqWebUI.Models.Filters;

public enum FilterPolicyKind
{
    Address,
    Server,
    RevServer,
    Local,
    StopDnsRebind,
    RebindLocalhostOk,
    RebindDomainOk,
    DomainNeeded,
    BogusPriv,
    BogusNxdomain,
    IgnoreAddress,
    FilterRr,
    FilterA,
    FilterAaaa,
    Filterwin2k,
    NoRoundRobin,
    Alias,
    Ipset,
    Nftset,
    ConnmarkAllowlistEnable,
    ConnmarkAllowlist
}
