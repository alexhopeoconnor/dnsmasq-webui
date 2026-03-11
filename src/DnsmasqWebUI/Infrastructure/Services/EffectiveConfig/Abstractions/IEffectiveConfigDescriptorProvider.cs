using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>
/// Canonical API for descriptor lookup and filtering. Scoped so UI and PageEditor share the same descriptor graph per status snapshot.
/// </summary>
public interface IEffectiveConfigDescriptorProvider : IApplicationScopedService
{
    /// <summary>All field descriptors for the given status (registry-built; may be cached per status for the scope).</summary>
    IReadOnlyList<EffectiveConfigFieldDescriptor> GetAll(DnsmasqServiceStatus status);

    /// <summary>Descriptors grouped by section, filtered by the given views (section + optional option allow-list).</summary>
    IReadOnlyDictionary<string, IReadOnlyList<EffectiveConfigFieldDescriptor>> GetBySection(
        DnsmasqServiceStatus status,
        IReadOnlyList<EffectiveConfigSectionView> views);

    /// <summary>Resolves a single descriptor by field ref; null if not found.</summary>
    EffectiveConfigFieldDescriptor? Resolve(DnsmasqServiceStatus status, EffectiveConfigFieldRef field);
}
