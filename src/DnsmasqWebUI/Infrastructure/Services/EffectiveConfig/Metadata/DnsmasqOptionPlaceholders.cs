using System.Collections.Frozen;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

/// <summary>
/// Placeholder values shown in effective-config editors before the user enters a value.
/// Kept separate from validation semantics and aligned with the existing option tooltip/help metadata pattern.
/// </summary>
public static class DnsmasqOptionPlaceholders
{
    private static readonly FrozenDictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // --- Hosts ---
        [DnsmasqConfKeys.AddnHosts] = "/etc/dnsmasq.hosts",
        [DnsmasqConfKeys.Hostsdir] = "/etc/dnsmasq.hosts.d",

        // --- Resolver / DNS ---
        [DnsmasqConfKeys.Server] = "IP or hostname, e.g. 8.8.8.8",
        [DnsmasqConfKeys.Local] = "/example.local/",
        [DnsmasqConfKeys.RevServer] = "CIDR,server, e.g. 1.2.3.0/24,192.168.1.1",
        [DnsmasqConfKeys.Address] = "/example.local/192.168.1.10",
        [DnsmasqConfKeys.ResolvFile] = "/etc/resolv.dnsmasq.conf",
        [DnsmasqConfKeys.FastDnsRetry] = "1000,10000",
        [DnsmasqConfKeys.RebindDomainOk] = "example.local",
        [DnsmasqConfKeys.BogusNxdomain] = "64.94.110.11",
        [DnsmasqConfKeys.IgnoreAddress] = "64.94.110.11",
        [DnsmasqConfKeys.Alias] = "192.168.0.0,10.0.0.0,255.255.255.0",
        [DnsmasqConfKeys.FilterRr] = "ANY",
        [DnsmasqConfKeys.AuthServer] = "example.local,eth0",
        [DnsmasqConfKeys.NoDhcpInterface] = "eth1",
        [DnsmasqConfKeys.NoDhcpv4Interface] = "eth1",
        [DnsmasqConfKeys.NoDhcpv6Interface] = "eth1",
        [DnsmasqConfKeys.Ipset] = "example.local/ipsetname",
        [DnsmasqConfKeys.Nftset] = "example.local#inet#filter#setname",
        [DnsmasqConfKeys.ConnmarkAllowlistEnable] = "mask, e.g. 0xff",
        [DnsmasqConfKeys.ConnmarkAllowlist] = "0xff,example.local",

        // --- DNS records ---
        [DnsmasqConfKeys.Domain] = "example.local,192.168.1.1",
        [DnsmasqConfKeys.Cname] = "router.example.local,router",
        [DnsmasqConfKeys.MxHost] = "example.local,mail.example.local,10",
        [DnsmasqConfKeys.MxTarget] = "mail.example.local",
        [DnsmasqConfKeys.Srv] = "_sip._tcp.example.local,sip.example.local,443,10,5",
        [DnsmasqConfKeys.PtrRecord] = "10.1.168.192.in-addr.arpa,router.example.local",
        [DnsmasqConfKeys.TxtRecord] = "example.local,\"hello world\"",
        [DnsmasqConfKeys.NaptrRecord] = "example.local,100,50,\"s\",\"SIP+D2U\",\"\",_sip._udp.example.local",
        [DnsmasqConfKeys.HostRecord] = "router.example.local,192.168.1.1",
        [DnsmasqConfKeys.DynamicHost] = "router.example.local,192.168.1.1,10m",
        [DnsmasqConfKeys.InterfaceName] = "router.example.local,eth0",
        [DnsmasqConfKeys.CaaRecord] = "example.local,0,issue,\"letsencrypt.org\"",
        [DnsmasqConfKeys.DnsRr] = "example.local,16,\"hello world\"",
        [DnsmasqConfKeys.SynthDomain] = "example.local,192.168.1.0/24,host-*",
        [DnsmasqConfKeys.AuthZone] = "example.local,192.168.1.0/24",
        [DnsmasqConfKeys.AuthSoa] = "ns1.example.local,hostmaster.example.local,1,3600,1200,604800",
        [DnsmasqConfKeys.AuthSecServers] = "example.local,ns2.example.local",
        [DnsmasqConfKeys.AuthPeer] = "192.168.1.2",

        // --- DHCP ---
        [DnsmasqConfKeys.Leasequery] = "IP[/prefix], e.g. 10.0.0.0/24",
        [DnsmasqConfKeys.DhcpGenerateNames] = "tag name, e.g. set:known",
        [DnsmasqConfKeys.DhcpBroadcast] = "tag:legacy-clients",
        [DnsmasqConfKeys.DhcpLeasefile] = "/var/lib/misc/dnsmasq.leases",
        [DnsmasqConfKeys.DhcpRange] = "start,end[,lease], e.g. 192.168.1.50,192.168.1.150,12h",
        [DnsmasqConfKeys.DhcpHost] = "aa:bb:cc:dd:ee:ff,192.168.1.10,host1,12h",
        [DnsmasqConfKeys.DhcpOption] = "option:dns-server,192.168.1.1",
        [DnsmasqConfKeys.DhcpOptionForce] = "option:dns-server,192.168.1.1",
        [DnsmasqConfKeys.DhcpMatch] = "set:tag,option:vendor-class,Example",
        [DnsmasqConfKeys.DhcpMac] = "set:tag,aa:bb:cc:*:*:*",
        [DnsmasqConfKeys.DhcpNameMatch] = "set:tag,hostname*",
        [DnsmasqConfKeys.DhcpIgnoreNames] = "tag:ignore-names",
        [DnsmasqConfKeys.DhcpHostsfile] = "/path/to/dhcp-hosts.conf",
        [DnsmasqConfKeys.DhcpOptsfile] = "/path/to/dhcp-opts.conf",
        [DnsmasqConfKeys.DhcpHostsdir] = "/path/to/dhcp-hosts.d",
        [DnsmasqConfKeys.DhcpOptsdir] = "/path/to/dhcp-opts.d",
        [DnsmasqConfKeys.DhcpBoot] = "pxelinux.0,,192.168.1.2",
        [DnsmasqConfKeys.DhcpIgnore] = "tag:blocked",
        [DnsmasqConfKeys.DhcpVendorclass] = "set:pxe,PXEClient",
        [DnsmasqConfKeys.DhcpUserclass] = "set:userclass,ExampleClient",
        [DnsmasqConfKeys.RaParam] = "eth0,mtu:1500,high",
        [DnsmasqConfKeys.Slaac] = "eth0,::10",
        [DnsmasqConfKeys.DhcpRelay] = "192.168.1.0,192.168.2.1",
        [DnsmasqConfKeys.DhcpCircuitid] = "set:<tag>,<circuit-id>",
        [DnsmasqConfKeys.DhcpRemoteid] = "set:<tag>,<remote-id>",
        [DnsmasqConfKeys.DhcpSubscrid] = "set:<tag>,<subscriber-id>",
        [DnsmasqConfKeys.DhcpProxy] = "192.168.1.0,192.168.1.1",
        [DnsmasqConfKeys.TagIf] = "tag:guest,tag:wifi",
        [DnsmasqConfKeys.BridgeInterface] = "br0,eth0",
        [DnsmasqConfKeys.SharedNetwork] = "sharednet,192.168.10.0,255.255.255.0",
        [DnsmasqConfKeys.BootpDynamic] = "tag:bootp",
        [DnsmasqConfKeys.DhcpAlternatePort] = "1067",
        [DnsmasqConfKeys.DhcpDuid] = "00:01:00:01:2a:11:22:33:44:55:66:77:88:99",
        [DnsmasqConfKeys.DhcpLuascript] = "/etc/dnsmasq/dhcp.lua",
        [DnsmasqConfKeys.DhcpScript] = "/usr/local/bin/dhcp-script.sh",
        [DnsmasqConfKeys.DhcpScriptuser] = "dnsmasq",
        [DnsmasqConfKeys.DhcpPxeVendor] = "PXEClient",

        // --- TFTP / PXE ---
        [DnsmasqConfKeys.TftpRoot] = "/srv/tftp",
        [DnsmasqConfKeys.PxePrompt] = "\"Boot menu\",5",
        [DnsmasqConfKeys.PxeService] = "x86PC,\"PXE Boot\",pxelinux",
        [DnsmasqConfKeys.DhcpOptionPxe] = "vendor:PXEClient,1,0.0.0.0",

        // --- DNSSEC ---
        [DnsmasqConfKeys.DnssecCheckUnsigned] = "no",
        [DnsmasqConfKeys.TrustAnchor] = ".,20326,8,2,e06d44b80b8f1d39...",
        [DnsmasqConfKeys.AddCpeId] = "my-cpe-id",
        [DnsmasqConfKeys.DnssecTimestamp] = "/var/lib/misc/dnsmasq.timestamp",
        [DnsmasqConfKeys.DnssecLimits] = "150,1500",

        // --- Cache ---
        [DnsmasqConfKeys.CacheRr] = "A,AAAA,TXT",
        [DnsmasqConfKeys.Dumpfile] = "/tmp/dnsmasq.dump",
        [DnsmasqConfKeys.Dumpmask] = "0x0000ffff",
        [DnsmasqConfKeys.UseStaleCache] = "seconds, e.g. 60",
        [DnsmasqConfKeys.AddMac] = "base64 or text",
        [DnsmasqConfKeys.AddSubnet] = "IPv4/IPv6 prefix, e.g. 24,96",
        [DnsmasqConfKeys.Umbrella] = "org-id,asset-id",

        // --- Process / networking ---
        [DnsmasqConfKeys.Interface] = "eth0",
        [DnsmasqConfKeys.ListenAddress] = "192.168.1.1",
        [DnsmasqConfKeys.ExceptInterface] = "lo",
        [DnsmasqConfKeys.LogAsync] = "queue size, e.g. 25",
        [DnsmasqConfKeys.PidFile] = "/run/dnsmasq.pid",
        [DnsmasqConfKeys.User] = "dnsmasq",
        [DnsmasqConfKeys.Group] = "dnsmasq",
        [DnsmasqConfKeys.LogFacility] = "local0",
        [DnsmasqConfKeys.EnableDbus] = "uk.org.thekelleys.dnsmasq",
        [DnsmasqConfKeys.EnableUbus] = "dnsmasq",
    }.ToFrozenDictionary();

    /// <summary>Returns the placeholder value for an option, or null when none is defined.</summary>
    public static string? Get(string optionName) =>
        Values.TryGetValue(optionName, out var value) ? value : null;
}
