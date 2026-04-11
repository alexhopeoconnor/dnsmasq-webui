using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords;

public sealed record AdvancedRecordTemplate(string OptionName, string Title, Func<DnsRecordRow?, string> PreviewBuilder);

public static class DnsRecordAdvancedTemplates
{
    public static readonly IReadOnlyList<AdvancedRecordTemplate> All =
    [
        new(DnsmasqConfKeys.NaptrRecord, "NAPTR",
            row => row?.Summary ?? "Route a service through NAPTR resolution (RFC 3403)."),
        new(DnsmasqConfKeys.CaaRecord, "CAA",
            row => row?.Summary ?? "Declare which CAs may issue certificates for this name."),
        new(DnsmasqConfKeys.DnsRr, "dns-rr",
            row => row?.Summary ?? "Arbitrary DNS resource record by type number and hex data."),
        new(DnsmasqConfKeys.DynamicHost, "dynamic-host",
            row => row?.Summary ?? "Publish a host name from a dynamically addressed interface."),
        new(DnsmasqConfKeys.InterfaceName, "interface-name",
            row => row?.Summary ?? "Map a DNS name to an interface address (localise-queries)."),
        new(DnsmasqConfKeys.SynthDomain, "synth-domain",
            row => row?.Summary ?? "Synthesize forward/reverse names from an address range."),
        new(DnsmasqConfKeys.AuthZone, "auth-zone",
            row => row?.Summary ?? "Serve a zone authoritatively for matching subnets."),
        new(DnsmasqConfKeys.AuthSoa, "auth-soa",
            row => row?.Summary ?? "SOA parameters for authoritative zones."),
        new(DnsmasqConfKeys.AuthSecServers, "auth-sec-servers",
            row => row?.Summary ?? "Secondary name servers allowed to AXFR."),
        new(DnsmasqConfKeys.AuthPeer, "auth-peer",
            row => row?.Summary ?? "Peer IPs allowed to initiate zone transfer."),
    ];
}
