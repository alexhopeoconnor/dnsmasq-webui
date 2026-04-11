using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>
/// Reattaches baseline source metadata to the current effective multi-value list produced by the edit session.
/// The result is intended for page projections and should not be treated as authoritative on-disk provenance.
/// </summary>
public interface IEffectiveMultiValueProjectionService : IApplicationSingleton
{
    IReadOnlyList<ProjectedMultiValueOccurrence> Project(
        IReadOnlyList<string> currentValues,
        IReadOnlyList<ValueWithSource>? baselineValues,
        string? managedFilePath);
}
