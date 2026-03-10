using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>synth-domain</c> values: &lt;domain&gt;,&lt;address range&gt;[,&lt;prefix&gt;[*]].
/// Domain and address range required; optional prefix (may end with *).
/// </summary>
public sealed class SynthDomainSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.SynthDomain;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "synth-domain value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length is < 2 or > 3)
            return "synth-domain must be <domain>,<address range>[,<prefix>[*]].";
        if (tokens.Any(t => t.Length == 0))
            return "synth-domain domain and address range cannot be empty.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "synth-domain domain must be a valid DNS name.";
        return null;
    }
}
