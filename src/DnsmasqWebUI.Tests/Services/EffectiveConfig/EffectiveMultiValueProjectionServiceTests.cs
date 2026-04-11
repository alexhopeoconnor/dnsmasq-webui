using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

public class EffectiveMultiValueProjectionServiceTests
{
    private readonly EffectiveMultiValueProjectionService _sut = new();

    [Fact]
    public void Project_ReusesBaselineSources_ByOccurrenceOrder()
    {
        var baseline = new[]
        {
            new ValueWithSource("same", new ConfigValueSource("/a.conf", "a.conf", true, 10)),
            new ValueWithSource("same", new ConfigValueSource("/b.conf", "b.conf", false, 20))
        };

        var projected = _sut.Project(["same", "same"], baseline, "/managed.conf");

        Assert.Equal("/a.conf", projected[0].Source?.FilePath);
        Assert.Equal("/b.conf", projected[1].Source?.FilePath);
        Assert.False(projected[0].IsDraftOnly);
        Assert.False(projected[1].IsDraftOnly);
    }

    [Fact]
    public void Project_UnmatchedRows_AreDraftEditableAndPointAtManagedPath()
    {
        var projected = _sut.Project(["draft-value"], [], "/managed.conf");

        var row = Assert.Single(projected);
        Assert.True(row.IsDraftOnly);
        Assert.True(row.IsEditable);
        Assert.Null(row.Source);
        Assert.Equal("/managed.conf", row.DisplaySourcePath);
        Assert.Equal("managed.conf", row.DisplaySourceLabel);
    }
}
