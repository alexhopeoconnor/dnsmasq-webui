using DnsmasqWebUI.Models.Config;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for DnsmasqConfOptionRegistry. Maps dnsmasq .conf option names to DnsmasqOptionKind.
/// </summary>
public class DnsmasqConfOptionRegistryTests
{
    [Fact]
    public void GetKind_EmptyOrWhitespace_ReturnsRaw()
    {
        Assert.Equal(DnsmasqOptionKind.Raw, DnsmasqConfOptionRegistry.GetKind(""));
        Assert.Equal(DnsmasqOptionKind.Raw, DnsmasqConfOptionRegistry.GetKind("  "));
    }

    [Fact]
    public void GetKind_ConfFile_ReturnsConfFile()
    {
        Assert.Equal(DnsmasqOptionKind.ConfFile, DnsmasqConfOptionRegistry.GetKind("conf-file"));
        Assert.Equal(DnsmasqOptionKind.ConfFile, DnsmasqConfOptionRegistry.GetKind("CONF-FILE"));
    }

    [Fact]
    public void GetKind_ConfDir_ReturnsConfDir()
    {
        Assert.Equal(DnsmasqOptionKind.ConfDir, DnsmasqConfOptionRegistry.GetKind("conf-dir"));
    }

    [Fact]
    public void GetKind_AddnHosts_ReturnsAddnHosts()
    {
        Assert.Equal(DnsmasqOptionKind.AddnHosts, DnsmasqConfOptionRegistry.GetKind("addn-hosts"));
    }

    [Fact]
    public void GetKind_DhcpLeasefile_ReturnsDhcpLeaseFile()
    {
        Assert.Equal(DnsmasqOptionKind.DhcpLeaseFile, DnsmasqConfOptionRegistry.GetKind("dhcp-leasefile"));
        Assert.Equal(DnsmasqOptionKind.DhcpLeaseFile, DnsmasqConfOptionRegistry.GetKind("dhcp-lease"));
    }

    [Fact]
    public void GetKind_Domain_ReturnsDomain()
    {
        Assert.Equal(DnsmasqOptionKind.Domain, DnsmasqConfOptionRegistry.GetKind("domain"));
    }

    [Fact]
    public void GetKind_ResolvFile_ReturnsPath()
    {
        Assert.Equal(DnsmasqOptionKind.Path, DnsmasqConfOptionRegistry.GetKind("resolv-file"));
    }

    [Fact]
    public void GetKind_FlagOptions_ReturnsFlag()
    {
        Assert.Equal(DnsmasqOptionKind.Flag, DnsmasqConfOptionRegistry.GetKind("domain-needed"));
        Assert.Equal(DnsmasqOptionKind.Flag, DnsmasqConfOptionRegistry.GetKind("bogus-priv"));
        Assert.Equal(DnsmasqOptionKind.Flag, DnsmasqConfOptionRegistry.GetKind("no-hosts"));
        Assert.Equal(DnsmasqOptionKind.Flag, DnsmasqConfOptionRegistry.GetKind("expand-hosts"));
        Assert.Equal(DnsmasqOptionKind.Flag, DnsmasqConfOptionRegistry.GetKind("bind-interfaces"));
        Assert.Equal(DnsmasqOptionKind.Flag, DnsmasqConfOptionRegistry.GetKind("log-queries"));
    }

    [Fact]
    public void GetKind_UnknownOption_ReturnsRaw()
    {
        Assert.Equal(DnsmasqOptionKind.Raw, DnsmasqConfOptionRegistry.GetKind("unknown-option"));
        Assert.Equal(DnsmasqOptionKind.Raw, DnsmasqConfOptionRegistry.GetKind("custom-key"));
    }

    [Fact]
    public void OptionKindByKey_ContainsExpectedOptions()
    {
        var registry = DnsmasqConfOptionRegistry.OptionKindByKey;
        Assert.True(registry.ContainsKey("addn-hosts"));
        Assert.True(registry.ContainsKey("dhcp-leasefile"));
        Assert.True(registry.ContainsKey("dhcp-lease"));
        Assert.True(registry.ContainsKey("conf-file"));
        Assert.True(registry.ContainsKey("conf-dir"));
        Assert.True(registry.ContainsKey("domain"));
        Assert.True(registry.ContainsKey("resolv-file"));
    }

    [Fact]
    public void FlagOptions_ContainsExpectedFlags()
    {
        var flags = DnsmasqConfOptionRegistry.FlagOptions;
        Assert.Contains("domain-needed", flags);
        Assert.Contains("no-hosts", flags);
        Assert.Contains("bind-interfaces", flags);
    }
}
