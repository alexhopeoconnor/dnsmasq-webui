using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Filters;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Filters.Abstractions;

public interface IFiltersPageProjectionService : IApplicationSingleton
{
    IReadOnlyList<FilterPolicyGroup> BuildGroups(DnsmasqServiceStatus status, FilterPolicyQueryState query);
}
