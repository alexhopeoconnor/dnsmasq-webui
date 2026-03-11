using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Editing.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Editing;

/// <summary>
/// Generic edit behavior for multi-value options that should reject duplicate items
/// while using the default trim-based normalization.
/// </summary>
public sealed class DistinctMultiValueEditBehavior : IMultiValueEditBehavior
{
    public bool AllowDuplicates => false;

    public string Normalize(string input) => (input ?? "").Trim();
}
