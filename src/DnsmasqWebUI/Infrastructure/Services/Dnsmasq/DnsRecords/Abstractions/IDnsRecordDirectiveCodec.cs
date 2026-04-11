using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords.Abstractions;

/// <summary>Parses and serializes one dnsmasq DNS-record directive value.</summary>
public interface IDnsRecordDirectiveCodec
{
    string OptionName { get; }
    DnsRecordFamily Family { get; }

    DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption);

    string Serialize(DnsRecordRow row);
}
