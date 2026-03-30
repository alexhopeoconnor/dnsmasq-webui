using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Rules;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig.CrossOption;

public class CrossOptionRulesTests
{
    private static EffectiveConfigCrossOptionContext Ctx(EffectiveDnsmasqConfig cfg) =>
        new(CrossOptionTestHelpers.Status(cfg), []);

    [Fact]
    public void NoResolvWithoutUpstreamsRule_Warns_when_no_server_and_no_resolv_file()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { NoResolv = true };
        var rule = new NoResolvWithoutUpstreamsRule();
        var issues = rule.Evaluate(Ctx(cfg));
        var key = EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Server);
        Assert.Single(issues);
        Assert.Equal(key, issues[0].FieldKey);
        Assert.Equal(FieldIssueSeverity.Warning, issues[0].Severity);
    }

    [Fact]
    public void NoResolvWithoutUpstreamsRule_No_issue_when_resolv_file_configured()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            NoResolv = true,
            ResolvFiles = ["/run/resolv.conf"]
        };
        var rule = new NoResolvWithoutUpstreamsRule();
        Assert.Empty(rule.Evaluate(Ctx(cfg)));
    }

    [Fact]
    public void ConntrackWithQueryPortRule_Error_when_both_set()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { Conntrack = true, QueryPort = 5353 };
        var rule = new ConntrackWithQueryPortRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Error, issues[0].Severity);
        Assert.Contains("query-port", issues[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RebindExceptionsRequireStopDnsRebindRule_Warns_on_localhost_ok_without_stop()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { RebindLocalhostOk = true };
        var rule = new RebindExceptionsRequireStopDnsRebindRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Warning, issues[0].Severity);
    }

    [Fact]
    public void BogusPrivBlocksPrivateReverseServerRule_Warns_for_private_in_addr_server()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            BogusPriv = true,
            ServerValues = ["/0.168.192.in-addr.arpa/10.0.0.1"]
        };
        var rule = new BogusPrivBlocksPrivateReverseServerRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Warning, issues[0].Severity);
        Assert.Equal(
            EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Server),
            issues[0].FieldKey);
    }

    [Theory]
    [InlineData("/10.in-addr.arpa/8.8.8.8")]
    [InlineData("/31.172.in-addr.arpa/8.8.8.8")]
    public void BogusPrivBlocksPrivateReverseServerRule_Warns_for_rfc1918_zone_roots(string serverValue)
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            BogusPriv = true,
            ServerValues = [serverValue]
        };
        var rule = new BogusPrivBlocksPrivateReverseServerRule();
        Assert.Single(rule.Evaluate(Ctx(cfg)));
    }

    [Theory]
    [InlineData("/210.in-addr.arpa/8.8.8.8")]
    [InlineData("/110.in-addr.arpa/8.8.8.8")]
    [InlineData("/1.0.0.192.in-addr.arpa/8.8.8.8")]
    [InlineData("/15.172.in-addr.arpa/8.8.8.8")]
    [InlineData("/32.172.in-addr.arpa/8.8.8.8")]
    [InlineData("/10.0.0.192.in-addr.arpa/8.8.8.8")]
    public void BogusPrivBlocksPrivateReverseServerRule_No_issue_for_non_private_in_addr_substrings(string serverValue)
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            BogusPriv = true,
            ServerValues = [serverValue]
        };
        var rule = new BogusPrivBlocksPrivateReverseServerRule();
        Assert.Empty(rule.Evaluate(Ctx(cfg)));
    }

    [Theory]
    [InlineData("192.168.1.0/24,8.8.8.8")]
    [InlineData("10.0.0.0/8,8.8.8.8")]
    [InlineData("172.16.0.0/12,8.8.8.8")]
    public void BogusPrivBlocksPrivateReverseServerRule_Warns_for_rfc1918_rev_server(string revServerValue)
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            BogusPriv = true,
            RevServerValues = [revServerValue]
        };
        var rule = new BogusPrivBlocksPrivateReverseServerRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(
            EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.RevServer),
            issues[0].FieldKey);
    }

    [Theory]
    [InlineData("8.8.8.0/24,1.1.1.1")]
    [InlineData("1.0.0.0/8,1.1.1.1")]
    public void BogusPrivBlocksPrivateReverseServerRule_No_issue_for_public_rev_server(string revServerValue)
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            BogusPriv = true,
            RevServerValues = [revServerValue]
        };
        var rule = new BogusPrivBlocksPrivateReverseServerRule();
        Assert.Empty(rule.Evaluate(Ctx(cfg)));
    }

    [Fact]
    public void BogusPrivBlocksPrivateReverseServerRule_Emits_separate_issues_for_server_and_rev_server()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            BogusPriv = true,
            ServerValues = ["/0.168.192.in-addr.arpa/10.0.0.1"],
            RevServerValues = ["192.168.2.0/24,8.8.8.8"]
        };
        var rule = new BogusPrivBlocksPrivateReverseServerRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Equal(2, issues.Count);
        Assert.Contains(
            issues,
            i => i.FieldKey == EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Server));
        Assert.Contains(
            issues,
            i => i.FieldKey == EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.RevServer));
    }

    [Fact]
    public void BogusPrivBlocksPrivateReverseServerRule_Pending_rev_server_overrides_disk()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            BogusPriv = true,
            RevServerValues = ["8.8.8.0/24,1.1.1.1"]
        };
        var pending = new[]
        {
            new PendingOptionChange(
                "resolver",
                DnsmasqConfKeys.RevServer,
                Array.Empty<string>(),
                new[] { "192.168.1.0/24,8.8.8.8" },
                null)
        };
        var ctx = new EffectiveConfigCrossOptionContext(CrossOptionTestHelpers.Status(cfg), pending);
        var rule = new BogusPrivBlocksPrivateReverseServerRule();
        var issues = rule.Evaluate(ctx);
        Assert.Single(issues);
        Assert.Equal(
            EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.RevServer),
            issues[0].FieldKey);
    }

    [Fact]
    public void NoPingWithDhcpSequentialIpRule_Warns_when_both_set()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            NoPing = true,
            DhcpSequentialIp = true
        };
        var rule = new NoPingWithDhcpSequentialIpRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Warning, issues[0].Severity);
        Assert.Equal(
            EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.DhcpSequentialIp),
            issues[0].FieldKey);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void NoPingWithDhcpSequentialIpRule_No_issue_unless_both(bool noPing, bool sequential)
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            NoPing = noPing,
            DhcpSequentialIp = sequential
        };
        var rule = new NoPingWithDhcpSequentialIpRule();
        Assert.Empty(rule.Evaluate(Ctx(cfg)));
    }

    [Fact]
    public void DnsPortZeroWithTftpOrPxeRule_Warns_when_port_zero_and_enable_tftp()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            Port = 0,
            EnableTftp = true
        };
        var rule = new DnsPortZeroWithTftpOrPxeRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(
            EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Port),
            issues[0].FieldKey);
    }

    [Fact]
    public void DnsPortZeroWithTftpOrPxeRule_Warns_when_port_zero_and_pxe_service()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            Port = 0,
            PxeServiceValues = ["x86PC,Boot from network,pxelinux"]
        };
        var rule = new DnsPortZeroWithTftpOrPxeRule();
        Assert.Single(rule.Evaluate(Ctx(cfg)));
    }

    [Fact]
    public void DnsPortZeroWithTftpOrPxeRule_No_issue_when_port_nonzero_even_with_netboot()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            Port = 53,
            EnableTftp = true,
            PxeServiceValues = ["x86PC,Boot from network,pxelinux"]
        };
        var rule = new DnsPortZeroWithTftpOrPxeRule();
        Assert.Empty(rule.Evaluate(Ctx(cfg)));
    }

    [Fact]
    public void DnsPortZeroWithTftpOrPxeRule_No_issue_when_port_zero_without_tftp_or_pxe()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { Port = 0 };
        var rule = new DnsPortZeroWithTftpOrPxeRule();
        Assert.Empty(rule.Evaluate(Ctx(cfg)));
    }

    [Fact]
    public void DnssecPrerequisitesRule_Error_when_build_lacks_dnssec()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { Dnssec = true };
        var status = CrossOptionTestHelpers.Status(cfg, dnsmasqSupportsDnssec: false);
        var ctx = new EffectiveConfigCrossOptionContext(status, []);
        var rule = new DnssecPrerequisitesRule();
        var issues = rule.Evaluate(ctx);
        var dnssecKey = EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Dnssec);
        Assert.Contains(issues, i =>
            i.Severity == FieldIssueSeverity.Error && i.FieldKey == dnssecKey);
    }

    [Fact]
    public void DnssecPrerequisitesRule_Warns_when_no_trust_anchors()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { Dnssec = true };
        var rule = new DnssecPrerequisitesRule();
        var issues = rule.Evaluate(Ctx(cfg));
        var trustAnchorKey = EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.TrustAnchor);
        Assert.Contains(issues, i =>
            i.Severity == FieldIssueSeverity.Warning &&
            i.FieldKey == trustAnchorKey &&
            i.Message.Contains("trust-anchor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProxyDnssecCacheWarningRule_Warns_when_cache_not_zero()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            ProxyDnssec = true,
            CacheSize = 150
        };
        var rule = new ProxyDnssecCacheWarningRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Warning, issues[0].Severity);
        Assert.Equal(
            EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.CacheSize),
            issues[0].FieldKey);
    }

    [Fact]
    public void ConnmarkAllowlistEnableRequiresAllowlistRule_Error_when_enable_key_only()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { ConnmarkAllowlistEnable = "" };
        var rule = new ConnmarkAllowlistEnableRequiresAllowlistRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Error, issues[0].Severity);
    }

    [Fact]
    public void ConnmarkAllowlistEnableRequiresAllowlistRule_No_issue_when_allowlists_present()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            ConnmarkAllowlistEnable = "0xff",
            ConnmarkAllowlistValues = ["0x1,*.example.com"]
        };
        var rule = new ConnmarkAllowlistEnableRequiresAllowlistRule();
        Assert.Empty(rule.Evaluate(Ctx(cfg)));
    }

    [Fact]
    public void QueryPortIgnoredForSourceBoundServerRule_Warns_when_server_has_at()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            QueryPort = 12345,
            ServerValues = ["10.0.0.1@192.168.1.1"]
        };
        var rule = new QueryPortIgnoredForSourceBoundServerRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Warning, issues[0].Severity);
    }

    [Fact]
    public void AddSubnetCacheBehaviorRule_Warns_when_add_subnet_set()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { AddSubnet = "24,96" };
        var rule = new AddSubnetCacheBehaviorRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(
            EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.AddSubnet),
            issues[0].FieldKey);
    }

    [Fact]
    public void Filterwin2kSrvWarningRule_Warns_when_enabled()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { Filterwin2k = true };
        var rule = new Filterwin2kSrvWarningRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Contains("SRV", issues[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddressLocalDnsmasq286CompatibilityRule_Warns_for_domain_address()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            AddressValues = ["/example.com/192.0.2.1"]
        };
        var rule = new AddressLocalDnsmasq286CompatibilityRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Warning, issues[0].Severity);
    }

    [Fact]
    public void AddressLocalDnsmasq286CompatibilityRule_No_issue_when_matching_local_exists()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            AddressValues = ["/example.com/192.0.2.1"],
            LocalValues = ["/example.com/"]
        };
        var rule = new AddressLocalDnsmasq286CompatibilityRule();
        Assert.Empty(rule.Evaluate(Ctx(cfg)));
    }

    [Fact]
    public void DnssecCheckUnsignedRequiresDnssecRule_Warns_when_dnssec_check_unsigned_set_without_dnssec()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            Dnssec = false,
            DnssecCheckUnsigned = ""
        };
        var rule = new DnssecCheckUnsignedRequiresDnssecRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Warning, issues[0].Severity);
        Assert.Equal(
            EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.DnssecCheckUnsigned),
            issues[0].FieldKey);
    }

    [Fact]
    public void LocalServiceIgnoredByBindSettingsRule_Warns_when_local_service_with_interface_filters()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            LocalService = "net",
            Interfaces = ["eth0"]
        };
        var rule = new LocalServiceIgnoredByBindSettingsRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Single(issues);
        Assert.Equal(FieldIssueSeverity.Warning, issues[0].Severity);
        Assert.Equal(
            EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.LocalService),
            issues[0].FieldKey);
    }

    [Fact]
    public void Pending_overlay_overrides_config_for_cross_option_rules()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { NoResolv = false };
        var pending = new[]
        {
            new PendingOptionChange("resolver", DnsmasqConfKeys.NoResolv, false, true, null)
        };
        var ctx = new EffectiveConfigCrossOptionContext(CrossOptionTestHelpers.Status(cfg), pending);
        var rule = new NoResolvWithoutUpstreamsRule();
        var issues = rule.Evaluate(ctx);
        Assert.Single(issues);
    }
}
