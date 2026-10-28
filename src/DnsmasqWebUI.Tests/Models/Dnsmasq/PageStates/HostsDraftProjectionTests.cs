using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Dnsmasq.PageStates;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Tests.Models.Dnsmasq.PageStates;

public class HostsDraftProjectionTests
{
    [Fact]
    public void From_PendingHostsOptionsProjectsUnsavedEffectiveState()
    {
        PendingDnsmasqChange[] pending =
        [
            new PendingOptionChange("hosts", DnsmasqConfKeys.NoHosts, false, true),
            new PendingOptionChange("hosts", DnsmasqConfKeys.ExpandHosts, false, true),
            new PendingOptionChange("dns", DnsmasqConfKeys.Domain, Array.Empty<string>(), new List<string>
            {
                "home.test",
                "lab.test,10.0.0.0/8"
            }),
        ];

        var projection = HostsDraftProjection.From(status: null, pending);

        Assert.True(projection.PageState.ExpandHostsEnabled);
        Assert.Equal("home.test", projection.PageState.ExpansionDomain);
        Assert.True(projection.HasPendingHostSourceConfiguration);
        Assert.Contains("pending draft enables no-hosts", projection.PageState.SystemHostsInactiveReason);
        Assert.Equal(["home.test", "lab.test,10.0.0.0/8"], projection.DomainValues);
    }

    [Fact]
    public void From_UnrelatedDraftDoesNotMarkHostSourceInventoryAsPending()
    {
        PendingDnsmasqChange[] pending =
        [
            new PendingOptionChange("dns", "cache-size", 150, 300),
        ];

        var projection = HostsDraftProjection.From(status: null, pending);

        Assert.False(projection.HasPendingHostSourceConfiguration);
        Assert.False(projection.PageState.ExpandHostsEnabled);
        Assert.Empty(projection.DomainValues);
    }

    [Fact]
    public void From_RemovedDomainClearsThePreviewDomain()
    {
        PendingDnsmasqChange[] pending =
        [
            new PendingOptionChange("hosts", DnsmasqConfKeys.ExpandHosts, false, true),
            new PendingOptionChange("dns", DnsmasqConfKeys.Domain, new[] { "home.test" }, null),
        ];

        var projection = HostsDraftProjection.From(status: null, pending);

        Assert.True(projection.PageState.ExpandHostsEnabled);
        Assert.Null(projection.PageState.ExpansionDomain);
        Assert.Empty(projection.DomainValues);
    }

    [Fact]
    public void ProjectSources_ReflectsPendingNoHostsWithoutChangingAdditionalSources()
    {
        var projection = HostsDraftProjection.From(
            status: null,
            [new PendingOptionChange("hosts", DnsmasqConfKeys.NoHosts, false, true)]);
        HostSource[] savedSources =
        [
            new(HostsRowSourceKind.SystemHosts, "/etc/hosts", "/etc/hosts", false, true, null, 0, null, 2),
            new(HostsRowSourceKind.AddnHosts, "/etc/dnsmasq.extra", "/etc/dnsmasq.extra", false, true, null, 1, null, 1),
        ];

        var projected = projection.ProjectSources(savedSources);

        Assert.False(projected[0].IsActive);
        Assert.Contains("pending draft enables no-hosts", projected[0].InactiveReason);
        Assert.Equal(savedSources[1], projected[1]);
    }
}
