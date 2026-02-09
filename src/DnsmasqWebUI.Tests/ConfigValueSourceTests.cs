using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

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
        var fileName = "02.conf";
        var fullPath = $"/etc/dnsmasq.d/{fileName}";
        var source = new ConfigValueSource(fullPath, fileName, IsManaged: false, LineNumber: null);
        var tooltip = source.GetReadOnlyTooltip();
        Assert.NotNull(tooltip);
        Assert.Contains(fileName, tooltip);
        Assert.Contains("readonly", tooltip);
        Assert.Contains(fullPath, tooltip);
        Assert.DoesNotContain("line", tooltip);
    }

    [Fact]
    public void GetReadOnlyTooltip_WhenReadOnlyWithLineNumber_IncludesLineNumber()
    {
        var fileName = "02.conf";
        var fullPath = $"/etc/dnsmasq.d/{fileName}";
        int? lineNumber = 3;
        var source = new ConfigValueSource(fullPath, fileName, IsManaged: false, LineNumber: lineNumber);
        var tooltip = source.GetReadOnlyTooltip();
        Assert.NotNull(tooltip);
        Assert.Contains(fileName, tooltip);
        Assert.Contains($"line {lineNumber}", tooltip);
        Assert.Contains("readonly", tooltip);
        Assert.Contains(fullPath, tooltip);
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
