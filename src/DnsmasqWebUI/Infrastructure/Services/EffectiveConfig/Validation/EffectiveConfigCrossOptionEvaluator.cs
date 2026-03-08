using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Evaluates cross-option rules on effective config (status merged with pending changes).
/// Returns validation issues (warnings/errors) for the UI and save guard.
/// </summary>
public static class EffectiveConfigCrossOptionEvaluator
{
    /// <summary>
    /// Runs all cross-option rules and returns issues to display. Caller should pass result to <see cref="IEffectiveConfigEditSession.SetCrossOptionIssues"/>.
    /// </summary>
    public static IReadOnlyList<FieldIssue> Evaluate(
        DnsmasqServiceStatus? status,
        IReadOnlyList<PendingEffectiveConfigChange> pending)
    {
        var issues = new List<FieldIssue>();

        var noResolv = GetEffectiveBool(status, pending, DnsmasqConfKeys.NoResolv, s => s?.EffectiveConfig?.NoResolv ?? false);
        var serverValues = GetEffectiveServerValues(status, pending);

        // no-resolv set with no upstream servers: DNS may not work
        if (noResolv && (serverValues == null || serverValues.Count == 0))
        {
            var fieldKey = $"{EffectiveConfigSections.SectionResolver}:{DnsmasqConfKeys.Server}";
            issues.Add(new FieldIssue(
                fieldKey,
                "When no-resolv is set, add at least one server or DNS may not work.",
                FieldIssueSeverity.Warning,
                ItemIndex: null));
        }

        // conntrack cannot be combined with query-port (dnsmasq man page)
        var conntrack = GetEffectiveBool(status, pending, DnsmasqConfKeys.Conntrack, s => s?.EffectiveConfig?.Conntrack ?? false);
        var queryPort = GetEffectiveInt(status, pending, DnsmasqConfKeys.QueryPort, s => s?.EffectiveConfig?.QueryPort);
        if (conntrack && queryPort is not null)
        {
            issues.Add(new FieldIssue(
                $"{EffectiveConfigSections.SectionResolver}:{DnsmasqConfKeys.QueryPort}",
                "query-port cannot be combined with conntrack.",
                FieldIssueSeverity.Error,
                null));
        }

        return issues;
    }

    private static bool GetEffectiveBool(
        DnsmasqServiceStatus? status,
        IReadOnlyList<PendingEffectiveConfigChange>? pending,
        string optionName,
        Func<DnsmasqServiceStatus?, bool> fromConfig)
    {
        var pendingChange = pending?.FirstOrDefault(c => string.Equals(c.OptionName, optionName, StringComparison.Ordinal));
        if (pendingChange != null)
        {
            if (pendingChange.NewValue is bool b)
                return b;
            return false; // pending change to clear or invalid; treat as off for cross-option purposes
        }
        return fromConfig(status);
    }

    private static int? GetEffectiveInt(
        DnsmasqServiceStatus? status,
        IReadOnlyList<PendingEffectiveConfigChange>? pending,
        string optionName,
        Func<DnsmasqServiceStatus?, int?> fromConfig)
    {
        var pendingChange = pending?.FirstOrDefault(c => string.Equals(c.OptionName, optionName, StringComparison.Ordinal));
        if (pendingChange != null)
        {
            if (pendingChange.NewValue is int i)
                return i;
            return null; // pending change to clear the value; use null so conntrack+query-port rule no longer blocks
        }
        return fromConfig(status);
    }

    private static IReadOnlyList<string>? GetEffectiveServerValues(DnsmasqServiceStatus? status, IReadOnlyList<PendingEffectiveConfigChange> pending)
    {
        var fromConfig = status?.EffectiveConfig?.ServerValues;
        var pendingChange = pending?.FirstOrDefault(c =>
            string.Equals(c.OptionName, DnsmasqConfKeys.Server, StringComparison.Ordinal));
        if (pendingChange?.NewValue is IReadOnlyList<string> list)
            return list;
        return fromConfig;
    }
}
