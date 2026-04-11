using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Filters;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Filters.Abstractions;

public interface IFilterPolicySummaryFormatter : IApplicationSingleton
{
    string Format(FilterPolicyKind kind, string? rawValue);
}
