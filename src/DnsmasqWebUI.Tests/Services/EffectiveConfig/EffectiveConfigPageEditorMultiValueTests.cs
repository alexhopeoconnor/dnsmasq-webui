using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

public class EffectiveConfigPageEditorMultiValueTests
{
    [Fact]
    public void ReplaceMultiValues_PreservesDuplicateValuesInPendingDraft()
    {
        var descriptor = new EffectiveConfigFieldDescriptor(
            EffectiveConfigSections.SectionResolver,
            DnsmasqConfKeys.Address,
            IsMultiValue: true,
            Status: null,
            ResolveValue: null,
            ResolveSource: null,
            ResolveItems: _ => [new ValueWithSource("/example/1.2.3.4", null)]);
        var session = new EffectiveConfigEditSession(new StubSaveService());
        var editor = new EffectiveConfigPageEditor(
            session,
            new StubDescriptorProvider(descriptor),
            new NoopCrossOptionValidationService());
        var field = editor.Field(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Address);

        editor.ReplaceMultiValues(null!, field, ["/example/1.2.3.4", "/example/1.2.3.4"]);

        var change = Assert.IsType<PendingOptionChange>(Assert.Single(session.Draft.PendingChanges));
        Assert.Equal(
            ["/example/1.2.3.4", "/example/1.2.3.4"],
            Assert.IsAssignableFrom<IReadOnlyList<string>>(change.NewValue));
    }

    private sealed class StubDescriptorProvider(EffectiveConfigFieldDescriptor descriptor) : IEffectiveConfigDescriptorProvider
    {
        public IReadOnlyList<EffectiveConfigFieldDescriptor> GetAll(DnsmasqServiceStatus status) => [descriptor];

        public IReadOnlyDictionary<string, IReadOnlyList<EffectiveConfigFieldDescriptor>> GetBySection(
            DnsmasqServiceStatus status,
            IReadOnlyList<EffectiveConfigSectionView> views) =>
            new Dictionary<string, IReadOnlyList<EffectiveConfigFieldDescriptor>>
            {
                [descriptor.SectionId] = [descriptor]
            };

        public EffectiveConfigFieldDescriptor? Resolve(DnsmasqServiceStatus status, EffectiveConfigFieldRef field) =>
            string.Equals(field.SectionId, descriptor.SectionId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(field.OptionName, descriptor.OptionName, StringComparison.OrdinalIgnoreCase)
                ? descriptor
                : null;
    }

    private sealed class NoopCrossOptionValidationService : IEffectiveConfigCrossOptionValidationService
    {
        public IReadOnlyList<FieldIssue> Validate(
            DnsmasqServiceStatus? status,
            IReadOnlyList<PendingOptionChange> pending) =>
            Array.Empty<FieldIssue>();
    }

    private sealed class StubSaveService : IEffectiveConfigSaveService
    {
        public Task<EffectiveConfigSaveResult> SaveAsync(
            IReadOnlyList<PendingDnsmasqChange> changes,
            CancellationToken ct = default) =>
            Task.FromResult(EffectiveConfigSaveResult.NoChanges());

        public Task<EffectiveConfigRestoreResult> RestoreAsync(
            IReadOnlyList<DnsmasqManagedBackup> backups,
            CancellationToken ct = default) =>
            Task.FromResult(new EffectiveConfigRestoreResult(false, false, -1, null, "Stub"));
    }
}
