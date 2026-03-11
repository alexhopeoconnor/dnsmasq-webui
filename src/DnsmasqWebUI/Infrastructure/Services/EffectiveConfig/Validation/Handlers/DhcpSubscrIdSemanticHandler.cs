using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>dhcp-subscrid</c> values: set:&lt;tag&gt;,&lt;subscriber-id&gt; (RFC3993).
/// </summary>
public sealed class DhcpSubscrIdSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpSubscrid;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value) =>
        DnsmasqDhcpTagSyntax.ValidateSetTagValue((value ?? "").Trim(), "dhcp-subscrid");
}
