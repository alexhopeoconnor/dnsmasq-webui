using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>dhcp-circuitid</c> values: set:&lt;tag&gt;,&lt;circuit-id&gt; (colon-hex or string).
/// </summary>
public sealed class DhcpCircuitidSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpCircuitid;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value) =>
        DnsmasqDhcpTagSyntax.ValidateSetTagValue((value ?? "").Trim(), "dhcp-circuitid");
}
