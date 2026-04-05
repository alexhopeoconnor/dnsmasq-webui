using DnsmasqWebUI.Components.EffectiveConfig;
using DnsmasqWebUI.Components.EffectiveConfig.CustomDisplays;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Rendering;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.AspNetCore.Components;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

public class EffectiveConfigRenderFragmentRegistryMultiTests
{
    [Fact]
    public void BuildMultiFieldComponentFragment_Domain_ReturnsFragment()
    {
        var registry = new EffectiveConfigRenderFragmentRegistry(new OptionSemanticValidator([new DomainSemanticHandler()]));
        var fragment = registry.BuildMultiFieldComponentFragment(
            EffectiveConfigSections.SectionHosts,
            DnsmasqConfKeys.Domain,
            EventCallback<IReadOnlyList<string>>.Empty);

        Assert.NotNull(fragment);
    }

    [Fact]
    public void DomainMultiValueDisplay_InheritsMultiBase()
    {
        Assert.Equal(typeof(EffectiveConfigMultiValueDisplayBase), typeof(DomainMultiValueDisplay).BaseType);
    }

    /// <summary>Custom domain UI must still use RegisterSemanticMultiDescriptor wiring so per-item validation is not dropped.</summary>
    [Fact]
    public void GetMultiDescriptorFactory_Domain_WiresSemanticValidator()
    {
        var registry = new EffectiveConfigRenderFragmentRegistry(new OptionSemanticValidator([new DomainSemanticHandler()]));
        var factory = registry.GetMultiDescriptorFactory(EffectiveConfigSections.SectionHosts, DnsmasqConfKeys.Domain);
        Assert.NotNull(factory);

        var descriptor = factory!(EffectiveConfigSections.SectionHosts, DnsmasqConfKeys.Domain, status: null, getItems: _ => null);
        Assert.NotNull(descriptor.Validator);
        Assert.Null(descriptor.Validator!.ValidateItem("example.com", Array.Empty<string>(), null));
        Assert.NotNull(descriptor.Validator.ValidateItem("not a valid domain name!", Array.Empty<string>(), null));
    }
}
