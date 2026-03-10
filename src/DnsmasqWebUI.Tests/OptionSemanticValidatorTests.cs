using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for the central semantic validator (handler dispatch and generic kind validation).
/// </summary>
public class OptionSemanticValidatorTests
{
    private readonly IOptionSemanticValidator _validator = new OptionSemanticValidator([
        new LeasequerySemanticHandler(),
        new ServerSemanticHandler(),
        new LocalSemanticHandler(),
        new RevServerSemanticHandler(),
        new AddressSemanticHandler(),
        new TrustAnchorSemanticHandler(),
        new AliasSemanticHandler(),
        new IpsetSemanticHandler(),
        new NftsetSemanticHandler(),
        new IgnoreAddressSemanticHandler(),
        new ConnmarkAllowlistSemanticHandler(),
        new DhcpRangeSemanticHandler(),
        new DhcpHostSemanticHandler(),
        new DhcpOptionSemanticHandler(),
        new DhcpMatchSemanticHandler(),
        new DhcpMacSemanticHandler(),
        new DhcpRelaySemanticHandler(),
        new DhcpProxySemanticHandler(),
        new RaParamSemanticHandler(),
        new DhcpNameMatchSemanticHandler(),
        new DhcpIgnoreSemanticHandler(),
        new DhcpVendorclassSemanticHandler(),
        new DhcpUserclassSemanticHandler(),
        new TagIfSemanticHandler(),
        new BridgeInterfaceSemanticHandler(),
        new SharedNetworkSemanticHandler(),
        new DhcpOptionPxeSemanticHandler(),
        new RebindDomainOkSemanticHandler(),
        new BogusNxdomainSemanticHandler(),
        new DhcpIgnoreNamesSemanticHandler(),
        new DhcpBootSemanticHandler(),
        new SlaacSemanticHandler(),
        new PxeServiceSemanticHandler(),
    ]);

    [Fact]
    public void ValidateMultiItem_LeasequeryHandler_InvalidIp_ReturnsError()
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex);
        Assert.NotNull(_validator.ValidateMultiItem(DnsmasqConfKeys.Leasequery, "not-an-ip", semantics));
        Assert.Null(_validator.ValidateMultiItem(DnsmasqConfKeys.Leasequery, "10.0.0.0/24", semantics));
        Assert.Null(_validator.ValidateMultiItem(DnsmasqConfKeys.Leasequery, "", semantics));
    }

    [Theory]
    [InlineData("1.2.3.4", true)]
    [InlineData("::1", true)]
    [InlineData("", true)]
    [InlineData("x.y.z", false)]
    public void ValidateMultiItem_IpAddressKind_AcceptsValid_RejectsInvalid(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.IpAddress, allowEmpty: true);
        var err = _validator.ValidateMultiItem("option", value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("", true, true)]
    [InlineData("x", true, true)]
    [InlineData("", false, false)]
    public void ValidateMultiItem_StringKind_RespectsAllowEmpty(string value, bool allowEmpty, bool expectValid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.String, allowEmpty: allowEmpty);
        var err = _validator.ValidateMultiItem("option", value, semantics);
        Assert.Equal(expectValid, err is null);
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("dns.example.com", true)]
    [InlineData("/internal.lan/192.168.2.1", true)]
    [InlineData("/google.com/#", true)]
    [InlineData("//", true)]
    [InlineData("", false)]
    [InlineData("http://bad", false)]
    [InlineData("/internal$lan/192.168.2.1", false)]
    [InlineData("/google.com/192.168.2.1#70000", false)]
    [InlineData("/google.com/@eth0", false)]
    public void ValidateMultiItem_Server_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.Server, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("::1", true)]
    [InlineData("", false)]
    [InlineData("dns.example.com", false)]
    public void ValidateMultiItem_ListenAddress_UsesIpAddressSemantics(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.IpAddress, allowEmpty: false);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.ListenAddress, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("1.2.3.0/24,192.168.1.1", true)]
    [InlineData("2001:db8::/64,2001:4860:4860::8888", true)]
    [InlineData("1.2.3.0/33,192.168.1.1", false)]
    [InlineData("not-an-ip/24,192.168.1.1", false)]
    [InlineData("1.2.3.0/x,192.168.1.1", false)]
    [InlineData("1.2.3.0/24,", false)]
    public void ValidateMultiItem_RevServer_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.RevServer, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("/example.local/192.168.1.10", true)]
    [InlineData("/#/1.2.3.4", true)]
    [InlineData("/example.local/#", true)]
    [InlineData("/example.local/", true)]
    [InlineData("example.local/192.168.1.10", false)]
    [InlineData("//192.168.1.10", true)]
    [InlineData("/example.local/not-an-ip", false)]
    public void ValidateMultiItem_Address_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.Address, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("/example.local/", true)]
    [InlineData("//", true)]
    [InlineData("/*.example.local/", true)]
    [InlineData("/internal$lan/", false)]
    [InlineData("/example.local/192.168.1.1", false)]
    public void ValidateMultiItem_Local_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.Local, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData(".,20326,8,2,abcdef", true)]
    [InlineData("example.com", true)]
    [InlineData("example.com,IN", true)]
    [InlineData("example.com,IN,20326,8,2,abcdef", true)]
    [InlineData("", false)]
    [InlineData("example.com,BOGUS", false)]
    [InlineData("example.com,IN,tag,8,2,abcdef", false)]
    public void ValidateMultiItem_TrustAnchor_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.TrustAnchor, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("1.2.3.0,6.7.8.0,255.255.255.0", true)]
    [InlineData("192.168.0.10-192.168.0.40,10.0.0.0,255.255.255.0", true)]
    [InlineData("192.168.0.10-,10.0.0.1", false)]
    [InlineData("not-an-ip,10.0.0.1", false)]
    public void ValidateMultiItem_Alias_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.Alias, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("/example.local/ipset1", true)]
    [InlineData("/example.local/example.org/ipset1,ipset2", true)]
    [InlineData("/internal$lan/ipset1", false)]
    [InlineData("/example.local/", false)]
    public void ValidateMultiItem_Ipset_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.Ipset, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("/example.local/inet#filter#set1", true)]
    [InlineData("/example.local/4#inet#filter#set1", true)]
    [InlineData("/example.local/6#inet#filter#set1", true)]
    [InlineData("/example.local/not#enough", false)]
    [InlineData("/internal$lan/inet#filter#set1", false)]
    public void ValidateMultiItem_Nftset_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.Nftset, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("64.94.110.11", true)]
    [InlineData("10.0.0.0/24", true)]
    [InlineData("2001:db8::/64", true)]
    [InlineData("not-an-ip", false)]
    public void ValidateMultiItem_IgnoreAddress_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.IgnoreAddress, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("example.com", true)]
    [InlineData("/domain1/domain2/", true)]
    [InlineData("/domain1//", false)]
    [InlineData("internal$lan", false)]
    public void ValidateMultiItem_RebindDomainOk_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.RebindDomainOk, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("64.94.110.11", true)]
    [InlineData("64.94.110.11/24", true)]
    [InlineData("not-an-ip", false)]
    public void ValidateMultiItem_BogusNxdomain_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.BogusNxdomain, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("0xff,example.com", true)]
    [InlineData("0xff/0xff,*", true)]
    [InlineData("0xff,*.example.com/api.example.com", true)]
    [InlineData("not-a-mark,example.com", false)]
    [InlineData("0xff,local", false)]
    public void ValidateMultiItem_ConnmarkAllowlist_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.ConnmarkAllowlist, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("192.168.1.50,192.168.1.150", true)]
    [InlineData("tag:guest,set:known,192.168.1.50,192.168.1.150,12h", true)]
    [InlineData("constructor:eth0,::,static", true)]
    [InlineData("172.28.0.10,", false)]
    [InlineData("172.28.0.10", false)]
    [InlineData("tag:guest,", false)]
    [InlineData("not-an-ip,192.168.1.150", false)]
    public void ValidateMultiItem_DhcpRange_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpRange, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("00:20:e0:3b:13:af,wap,infinite", true)]
    [InlineData("lap,192.168.0.199", true)]
    [InlineData("id:clientid,set:known,192.168.1.10,host1,12h", true)]
    [InlineData("ignore", false)]
    [InlineData("00:20:e0:3b:13:af,,host1", false)]
    public void ValidateMultiItem_DhcpHost_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpHost, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("3,192.168.4.4", true)]
    [InlineData("option:router,192.168.4.4", true)]
    [InlineData("vendor:PXEClient,1,0.0.0.0", true)]
    [InlineData("encap:175,190,iscsi-client0", true)]
    [InlineData("option:", false)]
    [InlineData("vendor:,", false)]
    public void ValidateMultiItem_DhcpOption_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpOption, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("set:efi-ia32,option:client-arch,6", true)]
    [InlineData("set:known,93", true)]
    [InlineData("option:client-arch,6", false)]
    [InlineData("set:,option:client-arch,6", false)]
    public void ValidateMultiItem_DhcpMatch_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpMatch, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("set:3com,01:34:23:*:*:*", true)]
    [InlineData("set:vendor,aa:bb:cc:dd:ee:ff", true)]
    [InlineData("01:34:23:*:*:*", false)]
    [InlineData("set:,01:34:23:*:*:*", false)]
    public void ValidateMultiItem_DhcpMac_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpMac, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("set:tag,hostname*", true)]
    [InlineData("set:tag,hostname", true)]
    [InlineData("set:tag,*host*", false)]
    [InlineData("hostname*", false)]
    public void ValidateMultiItem_DhcpNameMatch_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpNameMatch, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("tag:guest", true)]
    [InlineData("tag:guest,tag:lab", true)]
    [InlineData("guest", false)]
    public void ValidateMultiItem_DhcpIgnoreNames_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpIgnoreNames, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("tag:blocked", true)]
    [InlineData("tag:blocked,tag:!known", true)]
    [InlineData("blocked", false)]
    public void ValidateMultiItem_DhcpIgnore_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpIgnore, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("set:printers,Hewlett-Packard JetDirect", true)]
    [InlineData("printers,enterprise:32473,VendorClass", true)]
    [InlineData("set:printers,enterprise:notnum,VendorClass", false)]
    public void ValidateMultiItem_DhcpVendorclass_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpVendorclass, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("set:userclass,ExampleClient", true)]
    [InlineData("userclass,ExampleClient", true)]
    [InlineData("set:,ExampleClient", false)]
    public void ValidateMultiItem_DhcpUserclass_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpUserclass, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("set:ppp,tag:ppp*", true)]
    [InlineData("set:guest,tag:!known", true)]
    [InlineData("tag:known", false)]
    public void ValidateMultiItem_TagIf_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.TagIf, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("192.168.1.1,192.168.2.1", true)]
    [InlineData("192.168.1.1,192.168.2.1#1067,eth1", true)]
    [InlineData("192.168.1.1,eth1", true)]
    [InlineData("not-an-ip,192.168.2.1", false)]
    [InlineData("192.168.1.1,server.example.com", false)]
    public void ValidateMultiItem_DhcpRelay_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpRelay, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("192.168.1.1,192.168.1.2", true)]
    [InlineData("not-an-ip", false)]
    public void ValidateMultiItem_DhcpProxy_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpProxy, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("br0,eth0", true)]
    [InlineData("br0,tap*", true)]
    [InlineData("br0", false)]
    public void ValidateMultiItem_BridgeInterface_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.BridgeInterface, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("pxelinux.0", true)]
    [InlineData("tag:pxe,pxelinux.0,,192.168.1.2", true)]
    [InlineData("tag:,pxelinux.0", false)]
    [InlineData("", false)]
    public void ValidateMultiItem_DhcpBoot_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpBoot, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("sharednet,192.168.10.0,255.255.255.0", true)]
    [InlineData("eth0,192.168.10.1", true)]
    [InlineData("sharednet", false)]
    public void ValidateMultiItem_SharedNetwork_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.SharedNetwork, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("eth0,::10", true)]
    [InlineData("slaac", true)]
    [InlineData("ra-names,eth0", true)]
    [InlineData("bad value", false)]
    public void ValidateMultiItem_Slaac_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.Slaac, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("eth0,60", true)]
    [InlineData("eth0,mtu:1280,low,60,1200", true)]
    [InlineData("eth0,high", true)]
    [InlineData(",60", false)]
    [InlineData("eth0,mtu:", false)]
    public void ValidateMultiItem_RaParam_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.RaParam, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("x86PC,\"PXE Boot\",pxelinux", true)]
    [InlineData("tag:pxe,x86PC,\"PXE Boot\",pxelinux,192.168.1.2", true)]
    [InlineData("x86PC", false)]
    [InlineData("bad-csa,\"PXE Boot\",pxelinux", false)]
    public void ValidateMultiItem_PxeService_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.PxeService, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("vendor:PXEClient,1,0.0.0.0", true)]
    [InlineData("encap:175,190,iscsi-client0", true)]
    [InlineData("option:router,192.168.1.1", false)]
    [InlineData("vendor:,1,0.0.0.0", false)]
    public void ValidateMultiItem_DhcpOptionPxe_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.DhcpOptionPxe, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Fact]
    public void ValidateSingle_UseStaleCache_UsesEngineRule()
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.UseStaleCache, "", semantics));
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.UseStaleCache, "0", semantics));
        Assert.NotNull(_validator.ValidateSingle(DnsmasqConfKeys.UseStaleCache, "x", semantics));
    }

    [Fact]
    public void ValidateSingle_AddMac_UsesEngineRule()
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.AddMac, "", semantics));
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.AddMac, "base64", semantics));
        Assert.NotNull(_validator.ValidateSingle(DnsmasqConfKeys.AddMac, "bogus", semantics));
    }

    [Theory]
    [InlineData("")]
    [InlineData("24,96")]
    [InlineData("0/0")]
    public void ValidateSingle_AddSubnet_RemainsIntentionallyPermissive(string input)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.AddSubnet, input, semantics));
    }

    [Theory]
    [InlineData("")]
    [InlineData("org-id,asset-id")]
    [InlineData("device-123")]
    public void ValidateSingle_Umbrella_RemainsIntentionallyPermissive(string input)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.Umbrella, input, semantics));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("0xff", true)]
    [InlineData("255", true)]
    [InlineData("0xZZ", false)]
    [InlineData("-1", false)]
    public void ValidateSingle_ConnmarkAllowlistEnable_UsesEngineRule(string input, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        var err = _validator.ValidateSingle(DnsmasqConfKeys.ConnmarkAllowlistEnable, input, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("no", true)]
    [InlineData("yes", false)]
    public void ValidateSingle_DnssecCheckUnsigned_UsesEngineRule(string input, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        var err = _validator.ValidateSingle(DnsmasqConfKeys.DnssecCheckUnsigned, input, semantics);
        Assert.Equal(valid, err is null);
    }

    [Fact]
    public void ValidateSingle_IntKind_AcceptsIntOrNumericString()
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Int);
        Assert.Null(_validator.ValidateSingle("option", 42, semantics));
        Assert.Null(_validator.ValidateSingle("option", "99", semantics));
        Assert.NotNull(_validator.ValidateSingle("option", "abc", semantics));
    }

    [Fact]
    public void OptionValidationSemantics_PathPolicy_OnNonPathKind_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new OptionValidationSemantics(
                OptionValidationKind.Int,
                pathPolicy: PathExistencePolicy.MustExist));
    }

    [Fact]
    public void ValidateMultiItem_PathFile_RejectsDirectoryPath()
    {
        var validator = _validator;
        var semantics = new OptionValidationSemantics(
            OptionValidationKind.PathFile,
            allowEmpty: true,
            pathPolicy: PathExistencePolicy.MustExist);

        var dir = Directory.CreateTempSubdirectory("dnsmasq-webui-pathfile-");
        try
        {
            var err = validator.ValidateMultiItem(DnsmasqConfKeys.DhcpHostsfile, dir.FullName, semantics);
            Assert.Equal("File does not exist.", err);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ValidateMultiItem_PathDirectory_RejectsFilePath()
    {
        var validator = _validator;
        var semantics = new OptionValidationSemantics(
            OptionValidationKind.PathDirectory,
            allowEmpty: true,
            pathPolicy: PathExistencePolicy.MustExist);

        var filePath = Path.Combine(Path.GetTempPath(), $"dnsmasq-webui-pathdir-{Guid.NewGuid():N}.txt");
        File.WriteAllText(filePath, "test");
        try
        {
            var err = validator.ValidateMultiItem(DnsmasqConfKeys.DhcpHostsdir, filePath, semantics);
            Assert.Equal("Directory does not exist.", err);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ValidateMultiItem_PathFile_AcceptsExistingFile()
    {
        var validator = _validator;
        var semantics = new OptionValidationSemantics(
            OptionValidationKind.PathFile,
            allowEmpty: true,
            pathPolicy: PathExistencePolicy.MustExist);

        var filePath = Path.Combine(Path.GetTempPath(), $"dnsmasq-webui-pathfile-{Guid.NewGuid():N}.txt");
        File.WriteAllText(filePath, "test");
        try
        {
            var err = validator.ValidateMultiItem(DnsmasqConfKeys.DhcpHostsfile, filePath, semantics);
            Assert.Null(err);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ValidateMultiItem_PathDirectory_AcceptsExistingDirectory()
    {
        var validator = _validator;
        var semantics = new OptionValidationSemantics(
            OptionValidationKind.PathDirectory,
            allowEmpty: true,
            pathPolicy: PathExistencePolicy.MustExist);

        var dir = Directory.CreateTempSubdirectory("dnsmasq-webui-pathdir-");
        try
        {
            var err = validator.ValidateMultiItem(DnsmasqConfKeys.DhcpHostsdir, dir.FullName, semantics);
            Assert.Null(err);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
