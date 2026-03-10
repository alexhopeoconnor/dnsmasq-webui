using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>dhcp-subscrid</c> values: set:&lt;tag&gt;,&lt;subscriber-id&gt; (RFC3993).
/// </summary>
public sealed class DhcpSubscridSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpSubscrid;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value) =>
        DnsmasqDhcpTagSyntax.ValidateSetTagValue((value ?? "").Trim(), "dhcp-subscrid");
}
