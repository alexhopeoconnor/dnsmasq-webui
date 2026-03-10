using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>dhcp-remoteid</c> values: set:&lt;tag&gt;,&lt;remote-id&gt; (colon-hex or string).
/// </summary>
public sealed class DhcpRemoteidSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpRemoteid;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value) =>
        DnsmasqDhcpTagSyntax.ValidateSetTagValue((value ?? "").Trim(), "dhcp-remoteid");
}
