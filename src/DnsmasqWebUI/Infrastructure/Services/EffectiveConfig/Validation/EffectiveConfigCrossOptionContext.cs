using System.Linq;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Effective option values for cross-option rules: parsed config overlaid with pending edits.
/// </summary>
public sealed class EffectiveConfigCrossOptionContext
{
    private readonly Dictionary<string, PendingOptionChange> _pendingByOption;

    public DnsmasqServiceStatus? Status { get; }

    public EffectiveConfigCrossOptionContext(
        DnsmasqServiceStatus? status,
        IReadOnlyList<PendingOptionChange> pending)
    {
        Status = status;
        _pendingByOption = pending
            .GroupBy(p => p.OptionName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);
    }

    public bool GetBool(string optionName, Func<EffectiveDnsmasqConfig, bool> fromConfig)
    {
        if (_pendingByOption.TryGetValue(optionName, out var change))
        {
            if (change.NewValue is bool b)
                return b;
            return false;
        }

        return Status?.EffectiveConfig is { } cfg && fromConfig(cfg);
    }

    public int? GetInt(string optionName, Func<EffectiveDnsmasqConfig, int?> fromConfig)
    {
        if (_pendingByOption.TryGetValue(optionName, out var change))
        {
            if (change.NewValue is int i)
                return i;
            return null;
        }

        return Status?.EffectiveConfig is { } cfg ? fromConfig(cfg) : null;
    }

    public string? GetString(string optionName, Func<EffectiveDnsmasqConfig, string?> fromConfig)
    {
        if (_pendingByOption.TryGetValue(optionName, out var change))
            return change.NewValue?.ToString();

        return Status?.EffectiveConfig is { } cfg ? fromConfig(cfg) : null;
    }

    public IReadOnlyList<string> GetMulti(string optionName, Func<EffectiveDnsmasqConfig, IReadOnlyList<string>> fromConfig)
    {
        if (_pendingByOption.TryGetValue(optionName, out var change))
        {
            if (change.NewValue is IReadOnlyList<string> list)
                return list;
            // Pending row wins over disk: null or non-list (e.g. bad payload) => treat as empty multi-value,
            // same spirit as GetInt returning null / GetBool returning false when the pending shape does not match.
            return [];
        }

        return Status?.EffectiveConfig is { } cfg ? fromConfig(cfg) : [];
    }

    /// <summary>
    /// True when connmark-allowlist-enable is present (key-only uses empty string in effective config).
    /// </summary>
    public bool IsConnmarkAllowlistFilteringEnabled()
    {
        if (_pendingByOption.TryGetValue(DnsmasqConfKeys.ConnmarkAllowlistEnable, out var change))
        {
            if (change.NewValue == null)
                return false;
            if (change.NewValue is bool b)
                return b;
            return true;
        }

        return Status?.EffectiveConfig?.ConnmarkAllowlistEnable is not null;
    }

    /// <summary>Explicit section (e.g. attach an issue to a different field than the option that triggered the rule).</summary>
    public static string FieldKey(string sectionId, string optionName)
        => $"{sectionId}:{optionName}";

    /// <summary>
    /// Field key for <paramref name="optionName"/> using the same option → section mapping as the effective-config UI
    /// (<see cref="EffectiveConfigSections.GetSectionId"/>). Prefer this for cross-option rules so badges merge correctly.
    /// </summary>
    public static string FieldKeyForOption(string optionName) =>
        FieldKey(EffectiveConfigSections.GetSectionId(optionName), optionName);
}
