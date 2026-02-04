using DnsmasqWebUI.Models.EffectiveConfig;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for <see cref="ConfigValueSource"/> and <see cref="ConfigValueSource.GetReadOnlyTooltip"/>.
/// </summary>
public class ConfigValueSourceTests
{
    [Fact]
    public void GetReadOnlyTooltip_WhenManaged_ReturnsNull()
    {
        var source = new ConfigValueSource("/data/zz-managed.conf", "zz-managed.conf", IsManaged: true, LineNumber: 5);
        Assert.Null(source.GetReadOnlyTooltip());
    }

    [Fact]
    public void GetReadOnlyTooltip_WhenReadOnlyWithoutLineNumber_ReturnsFileNameOnly()
    {
        var source = new ConfigValueSource("/etc/dnsmasq.d/02.conf", "02.conf", IsManaged: false, LineNumber: null);
        var tooltip = source.GetReadOnlyTooltip();
        Assert.NotNull(tooltip);
        Assert.Contains("02.conf", tooltip);
        Assert.Contains("readonly", tooltip);
        Assert.Contains("/etc/dnsmasq.d/02.conf", tooltip);
        Assert.DoesNotContain("line", tooltip);
    }

    [Fact]
    public void GetReadOnlyTooltip_WhenReadOnlyWithLineNumber_IncludesLineNumber()
    {
        var source = new ConfigValueSource("/etc/dnsmasq.d/02.conf", "02.conf", IsManaged: false, LineNumber: 3);
        var tooltip = source.GetReadOnlyTooltip();
        Assert.NotNull(tooltip);
        Assert.Contains("02.conf", tooltip);
        Assert.Contains("line 3", tooltip);
        Assert.Contains("readonly", tooltip);
        Assert.Contains("/etc/dnsmasq.d/02.conf", tooltip);
    }

    [Fact]
    public void IsReadOnly_WhenManaged_IsFalse()
    {
        var source = new ConfigValueSource("/data/zz.conf", "zz.conf", IsManaged: true);
        Assert.False(source.IsReadOnly);
    }

    [Fact]
    public void IsReadOnly_WhenNotManaged_IsTrue()
    {
        var source = new ConfigValueSource("/etc/dnsmasq.conf", "dnsmasq.conf", IsManaged: false);
        Assert.True(source.IsReadOnly);
    }
}
