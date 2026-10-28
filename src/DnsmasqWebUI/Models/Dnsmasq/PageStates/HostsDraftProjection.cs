using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Models.Dnsmasq.PageStates;

/// <summary>
/// The Hosts-page view of the saved effective configuration plus its current shared draft.
/// Keeping this projection independent from Razor makes pending <c>no-hosts</c>,
/// <c>expand-hosts</c>, and <c>domain</c> changes consistently testable.
/// </summary>
public sealed record HostsDraftProjection(
    HostsPageState PageState,
    IReadOnlyList<string> DomainValues,
    bool HasPendingHostSourceConfiguration)
{
    /// <summary>
    /// Applies the draft's system-hosts activation state to a saved server source inventory.
    /// Pending <c>addn-hosts</c>/<c>hostsdir</c> paths are deliberately not invented here because
    /// they need a server-side disk scan after save; callers can still surface that pending state.
    /// </summary>
    public IReadOnlyList<HostSource> ProjectSources(IReadOnlyList<HostSource> sources) =>
        sources.Select(source =>
        {
            if (source.SourceKind != HostsRowSourceKind.SystemHosts)
                return source;

            return source with
            {
                IsActive = PageState.SystemHostsActive,
                InactiveReason = PageState.SystemHostsActive
                    ? null
                    : PageState.SystemHostsInactiveReason ?? source.InactiveReason
            };
        }).ToList();

    /// <summary>Builds the projection. A draft option replaces the corresponding saved effective value.</summary>
    public static HostsDraftProjection From(
        DnsmasqServiceStatus? status,
        IEnumerable<PendingDnsmasqChange>? pendingChanges)
    {
        var options = pendingChanges?
            .OfType<PendingOptionChange>()
            .ToList() ?? [];

        var noHosts = ResolveFlag(options, DnsmasqConfKeys.NoHosts, status?.NoHosts ?? false);
        var expandHosts = ResolveFlag(options, DnsmasqConfKeys.ExpandHosts, status?.EffectiveConfig?.ExpandHosts ?? false);
        var domainValues = ResolveDomainValues(options, status?.EffectiveConfig?.DomainValues ?? []);
        var saved = HostsPageState.FromStatus(status);
        var systemHostsAvailable = !string.IsNullOrWhiteSpace(status?.SystemHostsPath) && status!.SystemHostsPathExists;
        var systemHostsActive = !noHosts && systemHostsAvailable;
        var systemReason = noHosts
            ? "System hosts is ignored because the pending draft enables no-hosts."
            : !systemHostsAvailable
                ? saved.SystemHostsInactiveReason
                : null;

        var pageState = saved with
        {
            SystemHostsActive = systemHostsActive,
            SystemHostsInactiveReason = systemReason,
            ExpandHostsEnabled = expandHosts,
            ExpansionDomain = expandHosts ? domainValues.FirstOrDefault() : null
        };

        var hasPendingHostSourceConfiguration = options.Any(change =>
            string.Equals(change.OptionName, DnsmasqConfKeys.NoHosts, StringComparison.OrdinalIgnoreCase)
            || string.Equals(change.OptionName, DnsmasqConfKeys.AddnHosts, StringComparison.OrdinalIgnoreCase)
            || string.Equals(change.OptionName, DnsmasqConfKeys.Hostsdir, StringComparison.OrdinalIgnoreCase));

        return new HostsDraftProjection(pageState, domainValues, hasPendingHostSourceConfiguration);
    }

    private static bool ResolveFlag(
        IReadOnlyList<PendingOptionChange> options,
        string optionName,
        bool savedValue)
    {
        var pending = options.LastOrDefault(change =>
            string.Equals(change.OptionName, optionName, StringComparison.OrdinalIgnoreCase));
        if (pending == null)
            return savedValue;

        return pending.NewValue switch
        {
            bool value => value,
            string value when bool.TryParse(value, out var parsed) => parsed,
            string value => !string.IsNullOrWhiteSpace(value),
            null => false,
            _ => savedValue
        };
    }

    private static IReadOnlyList<string> ResolveDomainValues(
        IReadOnlyList<PendingOptionChange> options,
        IReadOnlyList<string> savedValues)
    {
        var pending = options.LastOrDefault(change =>
            string.Equals(change.OptionName, DnsmasqConfKeys.Domain, StringComparison.OrdinalIgnoreCase));
        if (pending == null)
            return savedValues;

        return pending.NewValue switch
        {
            IReadOnlyList<string> values => values,
            IEnumerable<string> values => values.ToList(),
            string value when !string.IsNullOrWhiteSpace(value) => [value],
            _ => []
        };
    }
}
