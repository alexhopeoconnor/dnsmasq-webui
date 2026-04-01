using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Tests.Models.EffectiveConfig;

public class ManagedHostsPendingChangeDiffTests
{
    [Fact]
    public void Build_same_entries_returns_empty()
    {
        var e = new HostEntry { Id = "a", LineNumber = 1, Address = "10.0.0.1", Names = new List<string> { "h" } };
        var oldL = new List<HostEntry> { e };
        var newL = new List<HostEntry>
        {
            new()
            {
                Id = e.Id,
                LineNumber = e.LineNumber,
                Address = e.Address,
                Names = new List<string>(e.Names)
            }
        };
        Assert.Empty(ManagedHostsPendingChangeDiff.Build(oldL, newL));
    }

    [Fact]
    public void Build_added_entry()
    {
        var oldL = new List<HostEntry>();
        var newL = new List<HostEntry>
        {
            new() { Id = "new:x", LineNumber = 1, Address = "192.168.0.1", Names = new List<string> { "router" } }
        };
        var d = ManagedHostsPendingChangeDiff.Build(oldL, newL);
        var a = Assert.Single(d);
        Assert.Equal(ManagedHostsLineDiffKind.Added, a.Kind);
        Assert.Contains("192.168.0.1", a.AfterLine, StringComparison.Ordinal);
        Assert.Contains("router", a.AfterLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_removed_entry()
    {
        var oldL = new List<HostEntry>
        {
            new() { Id = "1", LineNumber = 1, Address = "10.0.0.2", Names = new List<string> { "x" } }
        };
        var newL = new List<HostEntry>();
        var d = ManagedHostsPendingChangeDiff.Build(oldL, newL);
        var r = Assert.Single(d);
        Assert.Equal(ManagedHostsLineDiffKind.Removed, r.Kind);
        Assert.Contains("10.0.0.2", r.BeforeLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_modified_address()
    {
        var oldL = new List<HostEntry>
        {
            new() { Id = "k", LineNumber = 1, Address = "10.0.0.1", Names = new List<string> { "h" } }
        };
        var newL = new List<HostEntry>
        {
            new() { Id = "k", LineNumber = 1, Address = "10.0.0.9", Names = new List<string> { "h" } }
        };
        var d = ManagedHostsPendingChangeDiff.Build(oldL, newL);
        var m = Assert.Single(d);
        Assert.Equal(ManagedHostsLineDiffKind.Modified, m.Kind);
        Assert.Contains("10.0.0.1", m.BeforeLine, StringComparison.Ordinal);
        Assert.Contains("10.0.0.9", m.AfterLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_order_only()
    {
        var oldL = new List<HostEntry>
        {
            new() { Id = "a", LineNumber = 1, Address = "10.0.0.1", Names = new List<string> { "a" } },
            new() { Id = "b", LineNumber = 2, Address = "10.0.0.2", Names = new List<string> { "b" } }
        };
        var newL = new List<HostEntry>
        {
            new() { Id = "b", LineNumber = 1, Address = "10.0.0.2", Names = new List<string> { "b" } },
            new() { Id = "a", LineNumber = 2, Address = "10.0.0.1", Names = new List<string> { "a" } }
        };
        var d = ManagedHostsPendingChangeDiff.Build(oldL, newL);
        var o = Assert.Single(d);
        Assert.Equal(ManagedHostsLineDiffKind.OrderOnly, o.Kind);
    }
}
