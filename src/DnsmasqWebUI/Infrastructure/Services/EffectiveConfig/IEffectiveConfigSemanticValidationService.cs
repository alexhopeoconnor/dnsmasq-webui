using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

/// <summary>
/// Validates pending effective-config changes against option semantics before write.
/// Returns field-level issues (errors block save; warnings can be confirmed).
/// Registered as singleton via assembly scanning (<see cref="IApplicationSingleton"/>).
/// </summary>
public interface IEffectiveConfigSemanticValidationService : IApplicationSingleton
{
    /// <summary>Validates each change using option semantics. Returns all issues (errors and warnings).</summary>
    IReadOnlyList<FieldIssue> Validate(IReadOnlyList<PendingEffectiveConfigChange> changes);
}
