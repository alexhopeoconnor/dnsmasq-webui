using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;

public interface IEffectiveConfigCrossOptionRule : IApplicationMultiSingleton
{
    string Id { get; }

    IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context);
}
