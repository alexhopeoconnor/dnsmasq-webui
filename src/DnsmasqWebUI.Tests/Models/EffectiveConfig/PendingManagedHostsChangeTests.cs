using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Tests.Models.EffectiveConfig;

public class PendingManagedHostsChangeTests
{
    [Fact]
    public void HasChanges_WhenEntriesDiffer_ReturnsTrue()
    {
        var oldEntries = new List<HostEntry> { new() { Id = "1", Address = "192.168.1.1", Names = new List<string> { "host1" } } };
        var newEntries = new List<HostEntry> { new() { Id = "1", Address = "192.168.1.2", Names = new List<string> { "host1" } } };
        var pending = new PendingManagedHostsChange(oldEntries, newEntries, "/etc/hosts");
        Assert.True(pending.HasChanges);
    }

    [Fact]
    public void HasChanges_WhenEntriesSame_ReturnsFalse()
    {
        var entries = new List<HostEntry> { new() { Id = "1", Address = "192.168.1.1", Names = new List<string> { "host1" } } };
        var clone = entries.Select(e => new HostEntry { Id = e.Id, Address = e.Address, Names = new List<string>(e.Names) }).ToList();
        var pending = new PendingManagedHostsChange(entries, clone, "/etc/hosts");
        Assert.False(pending.HasChanges);
    }

    [Fact]
    public void HasChanges_WhenCountDiffers_ReturnsTrue()
    {
        var oldEntries = new List<HostEntry> { new() { Id = "1", Address = "192.168.1.1", Names = new List<string>() } };
        var newEntries = new List<HostEntry>
        {
            new() { Id = "1", Address = "192.168.1.1", Names = new List<string>() },
            new() { Id = "2", Address = "192.168.1.2", Names = new List<string>() }
        };
        var pending = new PendingManagedHostsChange(oldEntries, newEntries, "/etc/hosts");
        Assert.True(pending.HasChanges);
    }
}
