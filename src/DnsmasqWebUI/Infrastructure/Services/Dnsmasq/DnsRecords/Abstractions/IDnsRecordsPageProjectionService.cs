using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords.Abstractions;

/// <summary>Builds and filters typed rows for the DNS records page.</summary>
public interface IDnsRecordsPageProjectionService : IApplicationSingleton
{
    IReadOnlyList<DnsRecordRow> BuildRows(DnsmasqServiceStatus status);

    IReadOnlyList<DnsRecordRow> FilterRows(IReadOnlyList<DnsRecordRow> rows, DnsRecordsQueryState query);
}
