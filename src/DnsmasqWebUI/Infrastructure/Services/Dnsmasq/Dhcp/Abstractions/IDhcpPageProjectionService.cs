using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers.Abstractions;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dhcp.Ui;
using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Dhcp.Abstractions;

/// <summary>
/// Projects effective dhcp-host lines (plus sources and leases) into grouped, searchable page models.
/// </summary>
public interface IDhcpPageProjectionService : IApplicationSingleton
{
    IReadOnlyList<DhcpHostPageRow> BuildHostRows(
        DnsmasqServiceStatus status,
        IReadOnlyList<string> effectiveDhcpHostValues,
        IStructuredOptionValueHandler<DhcpHostEntry> handler,
        IReadOnlyList<LeaseEntry>? leases = null);

    IReadOnlyList<DhcpHostPageGroup> BuildHostGroups(
        IReadOnlyList<DhcpHostPageRow> rows,
        DhcpPageQueryState query,
        string? managedFilePath);

    DhcpExternalSourcesViewModel BuildExternalSources(DnsmasqServiceStatus status);

    IReadOnlyList<DhcpLeaseRowViewModel> BuildLeaseRows(
        DnsmasqServiceStatus status,
        IReadOnlyList<LeaseEntry>? leases,
        IReadOnlyList<DhcpHostPageRow> hostRows);

    IReadOnlyList<DhcpClassificationRuleRow> BuildClassificationRules(DnsmasqServiceStatus status);
}
