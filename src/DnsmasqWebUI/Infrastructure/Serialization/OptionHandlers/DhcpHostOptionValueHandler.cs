using DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers;

/// <summary>Canonical typed handler for dhcp-host= values. Replaces ad-hoc mapper; uses <see cref="DnsmasqConfDhcpHostLineParser"/> and <see cref="DnsmasqConfText"/>.</summary>
public sealed class DhcpHostOptionValueHandler : IStructuredOptionValueHandler<DhcpHostEntry>
{
    /// <inheritdoc />
    public string OptionName => DnsmasqConfKeys.DhcpHost;

    /// <inheritdoc />
    public Type ValueType => typeof(DhcpHostEntry);

    /// <inheritdoc />
    public string SerializeLine(DhcpHostEntry value) =>
        DnsmasqConfDhcpHostLineParser.ToLine(value);

    /// <inheritdoc />
    public string SerializeValue(DhcpHostEntry value)
    {
        var line = SerializeLine(value);
        var valuePart = DnsmasqConfText.StripDirectivePrefix(OptionName, line);
        return (valuePart is "#" or "##") ? "" : valuePart;
    }

    /// <inheritdoc />
    public bool TryParseValue(string text, int lineNumber, out DhcpHostEntry? value)
    {
        value = DnsmasqConfDhcpHostLineParser.ParseLine(
            DnsmasqConfText.DirectiveLine(OptionName, text),
            lineNumber);
        return value is not null;
    }
}
