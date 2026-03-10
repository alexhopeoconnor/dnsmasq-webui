using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>srv-host</c> values: &lt;_service&gt;.&lt;_prot&gt;[.&lt;domain&gt;],[&lt;target&gt;[,&lt;port&gt;[,&lt;priority&gt;[,&lt;weight&gt;]]]].
/// Service name required; optional target, port (0-65535), priority, weight.
/// </summary>
public sealed class SrvHostSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.Srv;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "srv-host value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length is 0 or > 5)
            return "srv-host must be <_service>._prot[.domain][,target[,port[,priority[,weight]]]].";
        if (tokens[0].Length == 0)
            return "srv-host service name cannot be empty.";
        if (!DnsmasqScopedDomainSyntax.IsValidSrvServiceName(tokens[0]))
            return "srv-host service name must be like _service._prot or _service._prot.domain.";
        if (tokens.Length >= 2 && tokens[1].Length > 0 && !DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[1]))
            return "srv-host target must be a valid DNS name or empty.";
        if (tokens.Length >= 3 && (!int.TryParse(tokens[2], out var port) || port < 0 || port > 65535))
            return "srv-host port must be 0-65535.";
        if (tokens.Length >= 4 && (!int.TryParse(tokens[3], out var pri) || pri < 0 || pri > 65535))
            return "srv-host priority must be 0-65535.";
        if (tokens.Length >= 5 && (!int.TryParse(tokens[4], out var w) || w < 0 || w > 65535))
            return "srv-host weight must be 0-65535.";
        return null;
    }
}
