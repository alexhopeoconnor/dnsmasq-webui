using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

namespace DnsmasqWebUI.Tests.Helpers;

/// <summary>
/// Sample config line per option for consistent parser coverage. When adding new options to the app,
/// add an entry here so the option-coverage test continues to exercise every option.
/// </summary>
public static class OptionCoverageData
{
    /// <summary>Option key and a minimal valid config line (without newline).</summary>
    public static IReadOnlyList<(string OptionKey, string ConfigLine)> GetParserCoverageEntries()
    {
        return new (string, string)[]
        {
            // LastWins
            (DnsmasqConfKeys.Port, "port=53"),
            (DnsmasqConfKeys.CacheSize, "cache-size=500"),
            (DnsmasqConfKeys.LocalTtl, "local-ttl=300"),
            (DnsmasqConfKeys.MxTarget, "mx-target=mail.example.com"),
            (DnsmasqConfKeys.DhcpLeasefile, "dhcp-leasefile=/var/lib/dnsmasq.leases"),
            (DnsmasqConfKeys.DhcpLeaseMax, "dhcp-lease-max=1000"),
            (DnsmasqConfKeys.Hostsdir, "hostsdir=/etc/dnsmasq.d"),
            (DnsmasqConfKeys.PidFile, "pid-file=/run/dnsmasq.pid"),
            (DnsmasqConfKeys.User, "user=nobody"),
            (DnsmasqConfKeys.TftpRoot, "tftp-root=/var/lib/tftpboot"),
            // Flag
            (DnsmasqConfKeys.NoHosts, "no-hosts"),
            (DnsmasqConfKeys.ExpandHosts, "expand-hosts"),
            (DnsmasqConfKeys.BogusPriv, "bogus-priv"),
            (DnsmasqConfKeys.NoResolv, "no-resolv"),
            (DnsmasqConfKeys.DomainNeeded, "domain-needed"),
            (DnsmasqConfKeys.StrictOrder, "strict-order"),
            (DnsmasqConfKeys.AllServers, "all-servers"),
            (DnsmasqConfKeys.BindInterfaces, "bind-interfaces"),
            (DnsmasqConfKeys.DhcpAuthoritative, "dhcp-authoritative"),
            (DnsmasqConfKeys.EnableTftp, "enable-tftp"),
            (DnsmasqConfKeys.Dnssec, "dnssec"),
            (DnsmasqConfKeys.Conntrack, "conntrack"),
            (DnsmasqConfKeys.Do0x20Encode, "do-0x20-encode"),
            (DnsmasqConfKeys.No0x20Encode, "no-0x20-encode"),
            // Multi - resolver / DNS
            (DnsmasqConfKeys.Server, "server=1.1.1.1"),
            (DnsmasqConfKeys.Local, "local=/lan/"),
            (DnsmasqConfKeys.RevServer, "rev-server=192.168.1.0/24,10.0.0.1"),
            (DnsmasqConfKeys.Address, "address=/example.com/127.0.0.1"),
            (DnsmasqConfKeys.Domain, "domain=home.lan,192.168.1.0/24"),
            (DnsmasqConfKeys.Cname, "cname=www.example.com,example.com"),
            (DnsmasqConfKeys.MxHost, "mx-host=example.com,mail.example.com,10"),
            (DnsmasqConfKeys.Srv, "srv-host=_http._tcp.example.com,host.example.com,80"),
            (DnsmasqConfKeys.PtrRecord, "ptr-record=1.168.192.in-addr.arpa,router.lan"),
            (DnsmasqConfKeys.TxtRecord, "txt-record=example.com,text"),
            (DnsmasqConfKeys.NaptrRecord, "naptr-record=example.com,0,0,a,s,r"),
            (DnsmasqConfKeys.DnsRr, "dns-rr=example.com,16,01:02"),
            (DnsmasqConfKeys.HostRecord, "host-record=host.example.com,1.2.3.4"),
            (DnsmasqConfKeys.DynamicHost, "dynamic-host=example.com"),
            (DnsmasqConfKeys.CaaRecord, "caa-record=example.com,0,issue,letsencrypt.org"),
            (DnsmasqConfKeys.RebindDomainOk, "rebind-domain-ok=example.com"),
            (DnsmasqConfKeys.BogusNxdomain, "bogus-nxdomain=192.168.1.1"),
            (DnsmasqConfKeys.IgnoreAddress, "ignore-address=192.168.1.1"),
            (DnsmasqConfKeys.Alias, "alias=0.0.0.0,example.com"),
            (DnsmasqConfKeys.FilterRr, "filter-rr=0.0.0.0,A"),
            (DnsmasqConfKeys.CacheRr, "cache-rr=0.0.0.0,A"),
            (DnsmasqConfKeys.Ipset, "ipset=/example.com/set1"),
            (DnsmasqConfKeys.Nftset, "nftset=/example.com/table/set"),
            (DnsmasqConfKeys.ConnmarkAllowlist, "connmark-allowlist=0xff,example.com"),
            // Multi - auth / synth
            (DnsmasqConfKeys.AuthServer, "auth-server=zone.example.com,eth0"),
            (DnsmasqConfKeys.SynthDomain, "synth-domain=dynamic.example.com,192.168.2.1,192.168.2.100"),
            (DnsmasqConfKeys.AuthZone, "auth-zone=example.com"),
            (DnsmasqConfKeys.TrustAnchor, "trust-anchor=.,20326,8,2,E4F1769B8A0E3142E125223716F878F0A3B2C3D4"),
            // Multi - DHCP
            (DnsmasqConfKeys.DhcpRange, "dhcp-range=192.168.1.50,192.168.1.150,12h"),
            (DnsmasqConfKeys.DhcpHost, "dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10"),
            (DnsmasqConfKeys.DhcpOption, "dhcp-option=3,192.168.1.1"),
            (DnsmasqConfKeys.DhcpOptionForce, "dhcp-option-force=option:dns-server,192.168.1.1"),
            (DnsmasqConfKeys.DhcpMatch, "dhcp-match=set:efi,option:client-arch,6"),
            (DnsmasqConfKeys.DhcpMac, "dhcp-mac=set:vendor,aa:bb:cc:dd:ee:ff"),
            (DnsmasqConfKeys.DhcpNameMatch, "dhcp-name-match=set:name,hostname*"),
            (DnsmasqConfKeys.DhcpIgnoreNames, "dhcp-ignore-names=tag:guest"),
            (DnsmasqConfKeys.DhcpBoot, "dhcp-boot=pxelinux.0"),
            (DnsmasqConfKeys.Leasequery, "leasequery=10.0.0.0/24"),
            (DnsmasqConfKeys.RaParam, "ra-param=eth0,high"),
            (DnsmasqConfKeys.Slaac, "slaac=eth0"),
            (DnsmasqConfKeys.PxeService, "pxe-service=x86PC,Install Linux"),
            (DnsmasqConfKeys.DhcpRelay, "dhcp-relay=192.168.1.1,eth0"),
            (DnsmasqConfKeys.DhcpCircuitid, "dhcp-circuitid=eth0,010203"),
            (DnsmasqConfKeys.DhcpRemoteid, "dhcp-remoteid=010203"),
            (DnsmasqConfKeys.DhcpSubscrid, "dhcp-subscrid=010203"),
            (DnsmasqConfKeys.TagIf, "tag-if=set:tag,eth0"),
            (DnsmasqConfKeys.SharedNetwork, "shared-network=name,192.168.1.0/24"),
            (DnsmasqConfKeys.DhcpOptionPxe, "dhcp-option-pxe=set:known,1"),
            (DnsmasqConfKeys.DhcpVendorclass, "dhcp-vendorclass=set:pxe,PXEClient"),
            (DnsmasqConfKeys.DhcpUserclass, "dhcp-userclass=set:efi,EFI"),
            (DnsmasqConfKeys.AddnHosts, "addn-hosts=/etc/hosts.extra"),
            (DnsmasqConfKeys.Interface, "interface=eth0"),
            (DnsmasqConfKeys.ListenAddress, "listen-address=127.0.0.1"),
            (DnsmasqConfKeys.ResolvFile, "resolv-file=/etc/resolv.dnsmasq"),
        };
    }
}
