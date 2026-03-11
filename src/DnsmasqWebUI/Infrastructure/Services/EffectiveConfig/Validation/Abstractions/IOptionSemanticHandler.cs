using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;

/// <summary>
/// Option-specific semantic handler for cases that generic validation kinds cannot express clearly.
/// Handlers centralize specialized validation by option name.
/// Registered as singleton collection via assembly scanning (<see cref="IApplicationMultiSingleton"/>).
/// </summary>
public interface IOptionSemanticHandler : IApplicationMultiSingleton
{
    /// <summary>True when this handler owns semantic behavior for the given option.</summary>
    bool CanHandle(string optionName);

    /// <summary>Validates a single-value option. Returns an error message or null if valid.</summary>
    string? ValidateSingle(object? value);

    /// <summary>Validates one item in a multi-value option. Returns an error message or null if valid.</summary>
    string? ValidateMultiItem(string? value);
}
