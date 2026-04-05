using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Tests.Models.Hosts;

public class HostsEffectiveNamesDomainRulesTests
{
    [Fact]
    public void Expand_UsesMatchingScopedDomainForIpv4()
    {
        var names = new[] { "router" };
        var domains = new[] { "home.lan", "corp.lan,10.20.0.0/16" };

        var result = HostsEffectiveNames.Expand(names, expandHosts: true, domains, "10.20.1.9");

        Assert.Equal(new[] { "router", "router.corp.lan" }, result);
    }

    [Fact]
    public void Expand_FallsBackToDefaultDomainWhenNoScopedMatch()
    {
        var names = new[] { "cache" };
        var domains = new[] { "home.lan", "corp.lan,10.20.0.0/16" };

        var result = HostsEffectiveNames.Expand(names, expandHosts: true, domains, "10.99.1.9");

        Assert.Equal(new[] { "cache", "cache.home.lan" }, result);
    }

    [Fact]
    public void Expand_UsesLastParsedScopedRuleWhenRangesOverlap()
    {
        var names = new[] { "app" };
        var domains = new[]
        {
            "home.lan",
            "corp-wide.lan,10.0.0.0/8",
            "corp-subnet.lan,10.20.0.0/16"
        };

        var result = HostsEffectiveNames.Expand(names, expandHosts: true, domains, "10.20.1.5");

        Assert.Equal(new[] { "app", "app.corp-subnet.lan" }, result);
    }

    [Fact]
    public void Expand_UsesDefaultDomainForIpv6Address()
    {
        var names = new[] { "node" };
        var domains = new[] { "home.lan", "corp.lan,10.20.0.0/16" };

        var result = HostsEffectiveNames.Expand(names, expandHosts: true, domains, "2001:db8::1");

        Assert.Equal(new[] { "node", "node.home.lan" }, result);
    }

    [Fact]
    public void ExplainSuffixSource_ScopedRuleMentionsRawLine()
    {
        var domains = new[] { "home.lan", "corp.lan,10.20.0.0/16" };
        var msg = HostsEffectiveNames.ExplainSuffixSource(
            expandHosts: true,
            domains,
            "10.20.1.9",
            new[] { "router" });

        Assert.Equal("Suffix from scoped rule: corp.lan,10.20.0.0/16", msg);
    }

    [Fact]
    public void ExplainSuffixSource_DefaultDomainWhenNoScopedMatch()
    {
        var domains = new[] { "home.lan", "corp.lan,10.20.0.0/16" };
        var msg = HostsEffectiveNames.ExplainSuffixSource(
            expandHosts: true,
            domains,
            "10.99.1.9",
            new[] { "cache" });

        Assert.Equal("Suffix from default rule: home.lan", msg);
    }

    [Fact]
    public void ExplainSuffixSource_ExpandHostsOff()
    {
        var msg = HostsEffectiveNames.ExplainSuffixSource(
            expandHosts: false,
            new[] { "home.lan" },
            "10.0.0.1",
            new[] { "a" });

        Assert.Contains("expand-hosts is off", msg);
    }
}
