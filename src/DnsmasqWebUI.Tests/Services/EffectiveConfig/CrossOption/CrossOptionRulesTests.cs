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
        var key = EffectiveConfigCrossOptionContext.FieldKey(
            EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Server);
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
    }

    [Fact]
    public void DnssecPrerequisitesRule_Error_when_build_lacks_dnssec()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { Dnssec = true };
        var status = CrossOptionTestHelpers.Status(cfg, dnsmasqSupportsDnssec: false);
        var ctx = new EffectiveConfigCrossOptionContext(status, []);
        var rule = new DnssecPrerequisitesRule();
        var issues = rule.Evaluate(ctx);
        Assert.Contains(issues, i => i.Severity == FieldIssueSeverity.Error);
    }

    [Fact]
    public void DnssecPrerequisitesRule_Warns_when_no_trust_anchors()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { Dnssec = true };
        var rule = new DnssecPrerequisitesRule();
        var issues = rule.Evaluate(Ctx(cfg));
        Assert.Contains(issues, i =>
            i.Severity == FieldIssueSeverity.Warning &&
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
