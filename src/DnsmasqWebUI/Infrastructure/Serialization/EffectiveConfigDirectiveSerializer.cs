using DnsmasqWebUI.Infrastructure.Serialization.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Serialization;

/// <summary>Serializes simple options to directive lines using <see cref="EffectiveConfigWriteSemantics"/> and <see cref="DnsmasqConfText"/>.</summary>
public sealed class EffectiveConfigDirectiveSerializer : IEffectiveConfigDirectiveSerializer
{
    /// <inheritdoc />
    public string SerializeSingle(string optionName, object? value)
    {
        return EffectiveConfigWriteSemantics.GetBehavior(optionName) switch
        {
            EffectiveConfigWriteBehavior.Flag =>
                (value is bool b && b) ? optionName : "",
            EffectiveConfigWriteBehavior.SingleValue =>
                DnsmasqConfText.DirectiveLine(optionName, value?.ToString()),
            EffectiveConfigWriteBehavior.KeyOnlyOrValue =>
                SerializeKeyOnlyOrValue(optionName, value),
            EffectiveConfigWriteBehavior.InversePair =>
                SerializeInversePair(optionName, value),
            _ => throw new InvalidOperationException($"Unsupported single-value write behavior for '{optionName}'.")
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<string> SerializeMulti(string optionName, IReadOnlyList<string> values)
    {
        return EffectiveConfigWriteSemantics.GetBehavior(optionName) switch
        {
            EffectiveConfigWriteBehavior.MultiValue =>
                values.Select(v => DnsmasqConfText.DirectiveLine(optionName, v)).ToList(),
            EffectiveConfigWriteBehavior.MultiKeyOnlyOrValue =>
                values.Select(v => string.IsNullOrWhiteSpace(v) ? optionName : DnsmasqConfText.DirectiveLine(optionName, v)).ToList(),
            _ => throw new InvalidOperationException($"Unsupported multi-value write behavior for '{optionName}'.")
        };
    }

    private static string SerializeInversePair(string optionName, object? value)
    {
        var pair = EffectiveConfigWriteSemantics.GetInversePairKeys(optionName);
        if (pair is null || value is not ExplicitToggleState state)
            return "";
        return state switch
        {
            ExplicitToggleState.Enabled => pair.Value.KeyA,
            ExplicitToggleState.Disabled => pair.Value.KeyB,
            _ => ""
        };
    }

    private static string SerializeKeyOnlyOrValue(string optionName, object? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text)
            ? optionName
            : DnsmasqConfText.DirectiveLine(optionName, text);
    }
}
