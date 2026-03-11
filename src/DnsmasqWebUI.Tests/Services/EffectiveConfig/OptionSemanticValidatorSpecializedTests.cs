using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>
/// Ensures every specialized option (one with a semantic handler) has at least one valid and one invalid
/// test case; asserts only Null/NotNull, not error message text.
/// </summary>
public class OptionSemanticValidatorSpecializedTests
{
    private static readonly OptionValidationSemantics ComplexAllowEmpty = new(OptionValidationKind.Complex, allowEmpty: true);

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
        new DhcpCircuitidSemanticHandler(),
        new DhcpRemoteidSemanticHandler(),
        new DhcpSubscrIdSemanticHandler(),
        new FilterRrSemanticHandler(),
        new CacheRrSemanticHandler(),
        new InterfaceNameSemanticHandler(),
        new AuthServerSemanticHandler(),
        new CnameSemanticHandler(),
        new MxHostSemanticHandler(),
        new PtrRecordSemanticHandler(),
        new InterfaceNameRecordSemanticHandler(),
        new CaaRecordSemanticHandler(),
        new SrvSemanticHandler(),
        new NaptrRecordSemanticHandler(),
        new DnsRrSemanticHandler(),
        new DynamicHostSemanticHandler(),
        new AuthSoaSemanticHandler(),
        new AuthSecServersSemanticHandler(),
        new AuthPeerSemanticHandler(),
        new HostRecordSemanticHandler(),
        new TxtRecordSemanticHandler(),
        new DomainSemanticHandler(),
        new SynthDomainSemanticHandler(),
        new AuthZoneSemanticHandler(),
    ]);

    /// <summary>Option key, one valid value (validator returns null), one invalid value (validator returns non-null).</summary>
    public static IEnumerable<object[]> GetSpecializedValidInvalidPairs()
    {
        yield return new object[] { DnsmasqConfKeys.Leasequery, "10.0.0.0/24", "not-an-ip" };
        yield return new object[] { DnsmasqConfKeys.Server, "1.1.1.1", "" };
        yield return new object[] { DnsmasqConfKeys.Local, "/lan/", "" };
        yield return new object[] { DnsmasqConfKeys.Address, "/example.com/127.0.0.1", "/bad..name/1.2.3.4" };
        yield return new object[] { DnsmasqConfKeys.Domain, "home.lan,192.168.1.0/24", "example.com,192.168.1.0/24,local,extra" };
        yield return new object[] { DnsmasqConfKeys.AuthServer, "zone.example.com,eth0", ",eth0" };
        yield return new object[] { DnsmasqConfKeys.SynthDomain, "dynamic.example.com,192.168.2.1,192.168.2.100", "example.com," };
        yield return new object[] { DnsmasqConfKeys.DnsRr, "example.com,16,01:02", "example.com,not-a-number" };
        yield return new object[] { DnsmasqConfKeys.Srv, "_http._tcp.example.com,host.example.com,80", "_sip._tcp,target,99999" };
        yield return new object[] { DnsmasqConfKeys.NaptrRecord, "example.com,0,0,a,s,r", "example.com,not-num,0,a,s,r" };
        yield return new object[] { DnsmasqConfKeys.TxtRecord, "example.com,text", "" };
        yield return new object[] { DnsmasqConfKeys.Cname, "www.example.com,example.com", "bad..name,example.com" };
        yield return new object[] { DnsmasqConfKeys.MxHost, "example.com,mail.example.com,10", "bad..name,mail.example.com,10" };
        yield return new object[] { DnsmasqConfKeys.PtrRecord, "1.168.192.in-addr.arpa,router.lan", "bad..arpa,host" };
        yield return new object[] { DnsmasqConfKeys.RebindDomainOk, "example.com", "bad..name" };
        yield return new object[] { DnsmasqConfKeys.BogusNxdomain, "192.168.1.1", "not-an-ip" };
        yield return new object[] { DnsmasqConfKeys.ConnmarkAllowlist, "0xff,example.com", "not-a-mark,example.com" };
        yield return new object[] { DnsmasqConfKeys.DhcpRange, "192.168.1.50,192.168.1.150,12h", "not-an-ip,192.168.1.150" };
        yield return new object[] { DnsmasqConfKeys.DhcpHost, "aa:bb:cc:dd:ee:ff,192.168.1.10", "ignore" };
        yield return new object[] { DnsmasqConfKeys.DhcpOption, "3,192.168.1.1", "option:," };
        yield return new object[] { DnsmasqConfKeys.DhcpMatch, "set:efi,option:client-arch,6", "option:client-arch,6" };
        yield return new object[] { DnsmasqConfKeys.DhcpMac, "set:vendor,aa:bb:cc:dd:ee:ff", "aa:bb:cc:dd:ee:ff" };
        yield return new object[] { DnsmasqConfKeys.DhcpNameMatch, "set:tag,hostname*", "hostname*" };
        yield return new object[] { DnsmasqConfKeys.DhcpIgnoreNames, "tag:guest", "guest" };
        yield return new object[] { DnsmasqConfKeys.AuthZone, "example.com", "bad..name" };
        yield return new object[] { DnsmasqConfKeys.FilterRr, "A,AAAA", "" };
        yield return new object[] { DnsmasqConfKeys.CacheRr, "A,TXT", "" };
    }

    [Theory]
    [MemberData(nameof(GetSpecializedValidInvalidPairs))]
    public void SpecializedOption_ValidAccepted_InvalidRejected(string optionKey, string validValue, string invalidValue)
    {
        var validErr = _validator.ValidateMultiItem(optionKey, validValue, ComplexAllowEmpty);
        Assert.Null(validErr);

        var invalidErr = _validator.ValidateMultiItem(optionKey, invalidValue, ComplexAllowEmpty);
        Assert.NotNull(invalidErr);
    }
}
