using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>
/// Builds the full effective-config descriptor graph for a status using the render-fragment registry.
/// Registered via DI so registry-backed building stays explicit and testable.
/// </summary>
public interface IEffectiveConfigFieldBuilder : IApplicationSingleton
{
    /// <summary>Builds all field descriptors for the given status.</summary>
    IReadOnlyList<EffectiveConfigFieldDescriptor> BuildFieldDescriptors(DnsmasqServiceStatus status);
}
