using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;

public interface IEffectiveConfigCrossOptionValidationService : IApplicationSingleton
{
    IReadOnlyList<FieldIssue> Validate(
        DnsmasqServiceStatus? status,
        IReadOnlyList<PendingOptionChange> pending);
}
