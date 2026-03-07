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

        var noResolv = GetEffectiveNoResolv(status, pending);
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

        return issues;
    }

    private static bool GetEffectiveNoResolv(DnsmasqServiceStatus? status, IReadOnlyList<PendingEffectiveConfigChange> pending)
    {
        var fromConfig = status?.EffectiveConfig?.NoResolv ?? false;
        var pendingChange = pending?.FirstOrDefault(c =>
            string.Equals(c.OptionName, DnsmasqConfKeys.NoResolv, StringComparison.Ordinal));
        if (pendingChange?.NewValue is bool b)
            return b;
        return fromConfig;
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
