using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Adapts a simple per-item validation delegate to <see cref="IMultiValueOptionValidator"/>.
/// </summary>
public sealed class DelegateMultiValueOptionValidator(Func<string?, string?> validate)
    : IMultiValueOptionValidator
{
    public string? ValidateItem(string normalized, IReadOnlyList<string> current, int? editIndex = null) =>
        validate(normalized);
}
