using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Models.Dnsmasq.PageStates;

/// <summary>
/// Page state for the Hosts specialized page. Derived from DnsmasqServiceStatus to keep Razor markup clean.
/// </summary>
public sealed record HostsPageState(
    bool ManagedHostsAvailable,
    string? ManagedHostsUnavailableReason,
    bool SystemHostsActive,
    string? SystemHostsInactiveReason,
    bool ExpandHostsEnabled,
    string? ExpansionDomain)
{
    /// <summary>When true, dnsmasq applies expand-hosts with a domain so effective names can differ from stored names.</summary>
    public bool ShowEffectiveNamesColumn =>
        ExpandHostsEnabled && !string.IsNullOrWhiteSpace(ExpansionDomain);

    public static HostsPageState FromStatus(DnsmasqServiceStatus? status)
    {
        if (status == null)
        {
            return new HostsPageState(
                ManagedHostsAvailable: false,
                ManagedHostsUnavailableReason: "Status not available.",
                SystemHostsActive: false,
                SystemHostsInactiveReason: "Status not available.",
                ExpandHostsEnabled: false,
                ExpansionDomain: null);
        }

        var managedHostsAvailable = !string.IsNullOrEmpty(status.ManagedHostsFilePath);
        var noHosts = status.NoHosts;
        var systemHostsActive = !noHosts && !string.IsNullOrEmpty(status.SystemHostsPath) && status.SystemHostsPathExists;
        var expandHosts = status.EffectiveConfig?.ExpandHosts ?? false;
        var domain = status.EffectiveConfig?.DomainValues?.FirstOrDefault();

        return new HostsPageState(
            ManagedHostsAvailable: managedHostsAvailable,
            ManagedHostsUnavailableReason: managedHostsAvailable
                ? null
                : "Hosts editing is unavailable: managed hosts path is not configured. Set Dnsmasq:MainConfigPath (and optionally Dnsmasq:ManagedHostsFileName) so the app can create and edit the managed hosts file.",
            SystemHostsActive: systemHostsActive,
            SystemHostsInactiveReason: noHosts
                ? "Dnsmasq is ignoring system hosts (no-hosts is enabled), so system hosts rows are hidden from this table."
                : string.IsNullOrEmpty(status.SystemHostsPath)
                    ? "System hosts path is not configured; system hosts rows are hidden from this table."
                    : !status.SystemHostsPathExists
                        ? $"System hosts file not found ({status.SystemHostsPath}); those rows are hidden from this table."
                        : null,
            ExpandHostsEnabled: expandHosts,
            ExpansionDomain: expandHosts ? domain : null);
    }
}
