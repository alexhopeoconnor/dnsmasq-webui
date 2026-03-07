namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Option-specific validation for a single item in a multi-value list.
/// </summary>
public interface IMultiValueOptionValidator
{
    /// <summary>
    /// Validates one normalized value. Returns an error message if invalid, or null if valid.
    /// </summary>
    /// <param name="normalized">The normalized string (e.g. after behavior.Normalize).</param>
    /// <param name="current">Current full list (for duplicate or context checks).</param>
    /// <param name="editIndex">Index being edited, or null when adding a new item.</param>
    string? ValidateItem(string normalized, IReadOnlyList<string> current, int? editIndex = null);
}
