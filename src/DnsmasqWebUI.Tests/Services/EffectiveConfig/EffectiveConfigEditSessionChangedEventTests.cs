using System.Linq;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;
using Microsoft.Extensions.Logging.Abstractions;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

public class EffectiveConfigEditSessionChangedEventTests
{
    [Fact]
    public void TrackCommit_RaisesChanged()
    {
        var saveService = new StubSaveService();
        var session = new EffectiveConfigEditSession(saveService);
        session.EnterEditMode();
        var raised = false;
        session.Changed += () => raised = true;
        session.TrackCommit(new EffectiveConfigEditCommittedArgs("resolver", "port", null, 5353, null));
        Assert.True(raised);
    }

    [Fact]
    public void EnterEditMode_RaisesChanged()
    {
        var saveService = new StubSaveService();
        var session = new EffectiveConfigEditSession(saveService);
        var raised = false;
        session.Changed += () => raised = true;
        session.EnterEditMode();
        Assert.True(raised);
    }

    [Fact]
    public void RevertChange_RaisesChanged()
    {
        var saveService = new StubSaveService();
        var session = new EffectiveConfigEditSession(saveService);
        session.EnterEditMode();
        session.TrackCommit(new EffectiveConfigEditCommittedArgs("resolver", "port", null, 5353, null));
        var raised = false;
        session.Changed += () => raised = true;
        session.RevertChange("resolver", "port");
        Assert.True(raised);
    }

    [Fact]
    public void TrackManagedHostsChange_StoresChange_AndRevertManagedHostsChange_RemovesIt()
    {
        var saveService = new StubSaveService();
        var session = new EffectiveConfigEditSession(saveService);
        session.EnterEditMode();
        var oldEntries = new List<HostEntry> { new() { Address = "1.2.3.4", Names = new List<string> { "a" } } };
        var newEntries = new List<HostEntry> { new() { Address = "1.2.3.4", Names = new List<string> { "a", "b" } } };
        var change = new PendingManagedHostsChange(oldEntries, newEntries, "/etc/hosts");
        session.TrackManagedHostsChange(change);
        Assert.Single(session.PendingChanges.OfType<PendingManagedHostsChange>());
        session.RevertManagedHostsChange();
        Assert.Empty(session.PendingChanges.OfType<PendingManagedHostsChange>());
    }

    private sealed class StubSaveService : IEffectiveConfigSaveService
    {
        public Task<EffectiveConfigSaveResult> SaveAsync(IReadOnlyList<PendingDnsmasqChange> changes, CancellationToken ct = default) =>
            Task.FromResult(EffectiveConfigSaveResult.NoChanges());
        public Task<EffectiveConfigRestoreResult> RestoreAsync(IReadOnlyList<DnsmasqManagedBackup> backups, CancellationToken ct = default) =>
            Task.FromResult(new EffectiveConfigRestoreResult(false, false, -1, null, "Stub"));
    }
}
