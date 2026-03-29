using System.Linq;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

public sealed class EffectiveConfigCrossOptionValidationService
    : IEffectiveConfigCrossOptionValidationService
{
    private readonly IReadOnlyList<IEffectiveConfigCrossOptionRule> _rules;

    public EffectiveConfigCrossOptionValidationService(IEnumerable<IEffectiveConfigCrossOptionRule> rules)
    {
        _rules = rules.ToList();
    }

    public IReadOnlyList<FieldIssue> Validate(
        DnsmasqServiceStatus? status,
        IReadOnlyList<PendingOptionChange> pending)
    {
        var context = new EffectiveConfigCrossOptionContext(status, pending);
        return _rules.SelectMany(r => r.Evaluate(context)).ToList();
    }
}
