using DnsmasqWebUI.Models;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for DnsmasqConfDirectiveParser. Parses a single dnsmasq .conf line into DnsmasqConfDirective
/// with typed option (AddnHostsOption, DhcpLeaseFileOption, PathOption, RawOption, etc.).
/// </summary>
public class DnsmasqConfDirectiveParserTests
{
    const string SourcePath = "/etc/dnsmasq.d/example.conf";

    [Fact]
    public void ParseLine_Empty_ReturnsNull()
    {
        Assert.Null(DnsmasqConfDirectiveParser.ParseLine("", 1, SourcePath));
        Assert.Null(DnsmasqConfDirectiveParser.ParseLine("   ", 2, SourcePath));
    }

    [Fact]
    public void ParseLine_Comment_ReturnsNull()
    {
        Assert.Null(DnsmasqConfDirectiveParser.ParseLine("# comment", 1, SourcePath));
        Assert.Null(DnsmasqConfDirectiveParser.ParseLine("# addn-hosts=/etc/hosts", 1, SourcePath));
    }

    [Fact]
    public void ParseLine_AddnHosts_ReturnsAddnHostsOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("addn-hosts=/var/lib/dnsmasq/hosts", 3, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(3, d!.LineNumber);
        Assert.Equal(SourcePath, d.SourceFilePath);
        Assert.Equal(DnsmasqOptionKind.AddnHosts, d.Kind);
        var opt = Assert.IsType<AddnHostsOption>(d.TypedOption);
        Assert.Equal("/var/lib/dnsmasq/hosts", opt.Path);
    }

    [Fact]
    public void ParseLine_AddnHosts_RelativePath_ResolvedAgainstSourceDir()
    {
        var conf = "/etc/dnsmasq.d/zz.conf";
        var d = DnsmasqConfDirectiveParser.ParseLine("addn-hosts=hosts.d/app.hosts", 1, conf);
        Assert.NotNull(d);
        var opt = Assert.IsType<AddnHostsOption>(d!.TypedOption);
        Assert.Equal(Path.GetFullPath("/etc/dnsmasq.d/hosts.d/app.hosts"), opt.Path);
    }

    [Fact]
    public void ParseLine_DhcpLeasefile_ReturnsDhcpLeaseFileOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("dhcp-leasefile=/var/lib/dnsmasq/dnsmasq.leases", 5, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.DhcpLeaseFile, d!.Kind);
        var opt = Assert.IsType<DhcpLeaseFileOption>(d.TypedOption);
        Assert.Equal("/var/lib/dnsmasq/dnsmasq.leases", opt.Path);
    }

    [Fact]
    public void ParseLine_DhcpLeaseFile_AlternativeKey_ReturnsDhcpLeaseFileOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("dhcp-lease-file=/run/dnsmasq.leases", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.DhcpLeaseFile, d!.Kind);
        var opt = Assert.IsType<DhcpLeaseFileOption>(d.TypedOption);
        Assert.Equal("/run/dnsmasq.leases", opt.Path);
    }

    [Fact]
    public void ParseLine_Domain_ReturnsDomainOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("domain=local", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.Domain, d!.Kind);
        var opt = Assert.IsType<DomainOption>(d.TypedOption);
        Assert.Equal("local", opt.Value);
    }

    [Fact]
    public void ParseLine_ConfFile_ReturnsConfFileOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("conf-file=/etc/dnsmasq.d/extra.conf", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.ConfFile, d!.Kind);
        var opt = Assert.IsType<ConfFileOption>(d.TypedOption);
        Assert.Equal("/etc/dnsmasq.d/extra.conf", opt.Path);
    }

    [Fact]
    public void ParseLine_ConfDir_ReturnsConfDirOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("conf-dir=/etc/dnsmasq.d", 2, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.ConfDir, d!.Kind);
        var opt = Assert.IsType<ConfDirOption>(d.TypedOption);
        Assert.Equal(Path.GetFullPath("/etc/dnsmasq.d"), opt.Path);
        Assert.Null(opt.Suffix);
    }

    [Fact]
    public void ParseLine_ConfDir_WithSuffix_ReturnsConfDirOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("conf-dir=/etc/dnsmasq.d,.conf", 1, SourcePath);
        Assert.NotNull(d);
        var opt = Assert.IsType<ConfDirOption>(d!.TypedOption);
        Assert.Equal(Path.GetFullPath("/etc/dnsmasq.d"), opt.Path);
        Assert.Equal(".conf", opt.Suffix);
    }

    [Fact]
    public void ParseLine_DhcpRange_ReturnsDhcpRangeOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("dhcp-range=192.168.1.100,192.168.1.200,255.255.255.0,12h", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.DhcpRange, d!.Kind);
        var opt = Assert.IsType<DhcpRangeOption>(d.TypedOption);
        Assert.Equal("192.168.1.100,192.168.1.200,255.255.255.0,12h", opt.RawValue);
    }

    [Fact]
    public void ParseLine_ResolvFile_ReturnsPathOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("resolv-file=/etc/resolv.dnsmasq.conf", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.Path, d!.Kind);
        var opt = Assert.IsType<PathOption>(d.TypedOption);
        Assert.Equal("resolv-file", opt.Key);
        Assert.Equal("/etc/resolv.dnsmasq.conf", opt.Path);
    }

    [Fact]
    public void ParseLine_FlagOption_ReturnsRawOptionWithEmptyValue()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("domain-needed", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.Flag, d!.Kind);
        var opt = Assert.IsType<RawOption>(d.TypedOption);
        Assert.Equal("domain-needed", opt.Key);
        Assert.Equal("", opt.Value);
    }

    [Fact]
    public void ParseLine_UnknownOption_ReturnsRawOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("unknown-option=foo", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.Raw, d!.Kind);
        var opt = Assert.IsType<RawOption>(d.TypedOption);
        Assert.Equal("unknown-option", opt.Key);
        Assert.Equal("foo", opt.Value);
    }

    [Fact]
    public void ParseLine_KeyOnly_ReturnsRawOptionWithEmptyValue()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("some-flag", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.Raw, d!.Kind);
        var opt = Assert.IsType<RawOption>(d.TypedOption);
        Assert.Equal("some-flag", opt.Key);
        Assert.Equal("", opt.Value);
    }

    [Fact]
    public void ParseLine_CommentedLine_ReturnsNull()
    {
        Assert.Null(DnsmasqConfDirectiveParser.ParseLine("#addn-hosts=/etc/hosts", 1, SourcePath));
    }

    [Fact]
    public void ParseLine_Server_ReturnsServerOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("server=8.8.8.8", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.Server, d!.Kind);
        var opt = Assert.IsType<ServerOption>(d.TypedOption);
        Assert.Equal("8.8.8.8", opt.RawValue);
    }

    [Fact]
    public void ParseLine_Local_ReturnsLocalOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("local=/localdomain/", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.Local, d!.Kind);
        var opt = Assert.IsType<LocalOption>(d.TypedOption);
        Assert.Equal("/localdomain/", opt.RawValue);
    }

    [Fact]
    public void ParseLine_Address_ReturnsAddressOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("address=/doubleclick.net/127.0.0.1", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.Address, d!.Kind);
        var opt = Assert.IsType<AddressOption>(d.TypedOption);
        Assert.Equal("/doubleclick.net/127.0.0.1", opt.RawValue);
    }

    [Fact]
    public void ParseLine_DhcpOption_ReturnsDhcpOptionOption()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("dhcp-option=option:router,192.168.1.1", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.DhcpOption, d!.Kind);
        var opt = Assert.IsType<DhcpOptionOption>(d.TypedOption);
        Assert.Equal("option:router,192.168.1.1", opt.RawValue);
    }

    [Fact]
    public void ParseLine_DhcpHost_ReturnsDhcpHostEntry()
    {
        var d = DnsmasqConfDirectiveParser.ParseLine("dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,testpc,infinite", 1, SourcePath);
        Assert.NotNull(d);
        Assert.Equal(DnsmasqOptionKind.DhcpHost, d!.Kind);
        var opt = Assert.IsType<DhcpHostEntry>(d.TypedOption);
        Assert.Single(opt.MacAddresses);
        Assert.Equal("aa:bb:cc:dd:ee:ff", opt.MacAddresses[0]);
        Assert.Equal("192.168.1.10", opt.Address);
    }
}
