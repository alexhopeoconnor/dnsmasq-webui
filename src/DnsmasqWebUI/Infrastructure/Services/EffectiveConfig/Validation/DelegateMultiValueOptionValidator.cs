using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Adapts a semantics multi-item validator delegate to <see cref="IMultiValueOptionValidator"/>.
/// </summary>
public sealed class DelegateMultiValueOptionValidator(EffectiveConfigMultiItemValidator validate)
    : IMultiValueOptionValidator
{
    public string? ValidateItem(string normalized, IReadOnlyList<string> current, int? editIndex = null) =>
        validate(normalized);
}
