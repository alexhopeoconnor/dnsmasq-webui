using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Rendering;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>
/// Wiring tests that ensure special-option semantics actually feed parser and registry behavior.
/// </summary>
public class EffectiveConfigSemanticsWiringTests
{
    [Fact]
    public void Registry_WiresSemanticValidator_ForDnsRr()
    {
        var registry = new EffectiveConfigRenderFragmentRegistry(new OptionSemanticValidator([new DnsRrSemanticHandler()]));
        var factory = registry.GetMultiDescriptorFactory(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.DnsRr);
        Assert.NotNull(factory);
        var descriptor = factory!(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.DnsRr, status: null, getItems: _ => null);
        Assert.NotNull(descriptor.Validator);
        Assert.Null(descriptor.Validator!.ValidateItem("example.com,16,01:02", Array.Empty<string>()));
        Assert.NotNull(descriptor.Validator.ValidateItem("example.com,not-a-number", Array.Empty<string>()));
    }

    [Fact]
    public void Registry_WiresLeasequeryMultiValidator_FromSpecialSemantics()
    {
        var registry = new EffectiveConfigRenderFragmentRegistry(new OptionSemanticValidator([new LeasequerySemanticHandler()]));
        var factory = registry.GetMultiDescriptorFactory(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.Leasequery);
        Assert.NotNull(factory);

        var descriptor = factory!(
            EffectiveConfigFieldBuilder.SectionDhcp,
            DnsmasqConfKeys.Leasequery,
            status: null,
            getItems: _ => null);

        Assert.NotNull(descriptor.Validator);
        Assert.NotNull(descriptor.Validator!.ValidateItem("not-an-ip", Array.Empty<string>()));
        Assert.Null(descriptor.Validator.ValidateItem("10.0.0.0/24", Array.Empty<string>()));
    }

    /// <summary>Pre-save semantic blocking: invalid leasequery value produces errors; SaveService uses this and returns SemanticValidationFailed before write.</summary>
    [Fact]
    public void SemanticValidationService_InvalidLeasequery_ReturnsError()
    {
        var validator = new OptionSemanticValidator([new LeasequerySemanticHandler()]);
        var service = new EffectiveConfigSemanticValidationService(validator);
        var changes = new List<PendingEffectiveConfigChange>
        {
            new(EffectiveConfigSections.SectionDhcp, DnsmasqConfKeys.Leasequery, null,
                new List<string> { "not-an-ip" }, null)
        };
        var issues = service.Validate(changes);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, i => i.Severity == FieldIssueSeverity.Error);
    }
}
