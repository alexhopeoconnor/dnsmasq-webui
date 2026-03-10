using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Central validator for effective-config option values driven by <see cref="OptionValidationSemantics"/>.
/// Used by the registry for field validation and by the pre-save semantic validation pass.
/// Registered as singleton via assembly scanning (<see cref="IApplicationSingleton"/>).
/// </summary>
public interface IOptionSemanticValidator : IApplicationSingleton
{
    /// <summary>Validates a single-value field. Returns error message or null if valid.</summary>
    string? ValidateSingle(string optionName, object? value, OptionValidationSemantics semantics);

    /// <summary>Validates one item in a multi-value list. Returns error message or null if valid.</summary>
    string? ValidateMultiItem(string optionName, string? value, OptionValidationSemantics semantics);
}
