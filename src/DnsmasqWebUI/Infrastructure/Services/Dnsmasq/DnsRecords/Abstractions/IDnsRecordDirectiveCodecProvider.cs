using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords.Abstractions;

/// <summary>Resolves <see cref="IDnsRecordDirectiveCodec"/> by dnsmasq option name.</summary>
public interface IDnsRecordDirectiveCodecProvider : IApplicationSingleton
{
    IDnsRecordDirectiveCodec Get(string optionName);

    bool TryGet(string optionName, out IDnsRecordDirectiveCodec? codec);

    /// <summary>Option names in DNS records section display order.</summary>
    IReadOnlyList<string> DnsRecordsSectionOptionNames { get; }
}
