using System.Linq;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords;

/// <inheritdoc />
public sealed class DnsRecordDirectiveCodecProvider : IDnsRecordDirectiveCodecProvider
{
    private readonly IReadOnlyDictionary<string, IDnsRecordDirectiveCodec> _map;

    public DnsRecordDirectiveCodecProvider()
    {
        IDnsRecordDirectiveCodec[] codecs =
        [
            new CnameRecordCodec(),
            new MxHostRecordCodec(),
            new SrvRecordCodec(),
            new PtrRecordCodec(),
            new TxtRecordCodec(),
            new NaptrRecordCodec(),
            new HostRecordCodec(),
            new DynamicHostRecordCodec(),
            new InterfaceNameRecordCodec(),
            new CaaRecordCodec(),
            new DnsRrRecordCodec(),
            new SynthDomainRecordCodec(),
            new AuthZoneRecordCodec(),
            new AuthSoaRecordCodec(),
            new AuthSecServersRecordCodec(),
            new AuthPeerRecordCodec(),
        ];
        _map = codecs.ToDictionary(c => c.OptionName, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> DnsRecordsSectionOptionNames { get; } =
        EffectiveConfigSections.Sections
            .First(s => s.SectionId == EffectiveConfigSections.SectionDnsRecords)
            .OptionNames;

    /// <inheritdoc />
    public IDnsRecordDirectiveCodec Get(string optionName) =>
        TryGet(optionName, out var c) && c != null
            ? c
            : throw new ArgumentException($"No codec for option '{optionName}'.", nameof(optionName));

    /// <inheritdoc />
    public bool TryGet(string optionName, out IDnsRecordDirectiveCodec? codec) =>
        _map.TryGetValue(optionName, out codec);
}
