using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Rules;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig.CrossOption;

public class EffectiveConfigCrossOptionValidationServiceTests
{
    [Fact]
    public void Validate_aggregates_issues_from_all_rules()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with
        {
            Conntrack = true,
            QueryPort = 1,
            Filterwin2k = true
        };
        var status = CrossOptionTestHelpers.Status(cfg);
        IReadOnlyList<IEffectiveConfigCrossOptionRule> rules =
        [
            new ConntrackWithQueryPortRule(),
            new Filterwin2kSrvWarningRule()
        ];
        var service = new EffectiveConfigCrossOptionValidationService(rules);

        var issues = service.Validate(status, []);

        Assert.Equal(2, issues.Count);
    }
}
