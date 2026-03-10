namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Option-specific behavior for the multi-value list editor: duplicate policy and normalization.
/// </summary>
public interface IMultiValueEditBehavior
{
    /// <summary>Whether duplicate values are allowed in the list.</summary>
    bool AllowDuplicates { get; }

    /// <summary>Normalizes raw input (e.g. trim, lowercase for domains).</summary>
    string Normalize(string input);
}
