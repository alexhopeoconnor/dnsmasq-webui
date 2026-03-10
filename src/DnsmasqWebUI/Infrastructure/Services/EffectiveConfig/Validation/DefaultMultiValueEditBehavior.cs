namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Default behavior for multi-value list editor: allow duplicates and trim normalization.
/// </summary>
public sealed class DefaultMultiValueEditBehavior : IMultiValueEditBehavior
{
    public bool AllowDuplicates => true;
    public string Normalize(string input) => (input ?? "").Trim();
}
