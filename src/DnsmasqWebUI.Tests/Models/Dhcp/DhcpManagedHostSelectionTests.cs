using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Tests.Models.Dhcp;

public sealed class DhcpManagedHostSelectionTests
{
    [Fact]
    public void Single_removal_shifts_higher_indices()
    {
        var s = new HashSet<int> { 0, 2, 4 };
        DhcpManagedHostSelection.RemapAfterRemovals(s, new[] { 1 });
        Assert.Equal(new[] { 0, 1, 3 }, s.Order().ToArray());
    }

    [Fact]
    public void Bulk_removal_removes_and_shifts()
    {
        var s = new HashSet<int> { 0, 2, 4 };
        DhcpManagedHostSelection.RemapAfterRemovals(s, new[] { 1, 3, 5 });
        Assert.Equal(new[] { 0, 1, 2 }, s.Order().ToArray());
    }

    [Fact]
    public void Removing_edited_index_drops_it_from_selection()
    {
        var s = new HashSet<int> { 1, 3 };
        DhcpManagedHostSelection.RemapAfterRemovals(s, new[] { 1 });
        Assert.Equal(new[] { 2 }, s.Order().ToArray());
    }

    [Fact]
    public void Empty_removed_is_noop()
    {
        var s = new HashSet<int> { 1, 2 };
        DhcpManagedHostSelection.RemapAfterRemovals(s, Array.Empty<int>());
        Assert.Equal(new[] { 1, 2 }, s.Order().ToArray());
    }
}
