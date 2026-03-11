using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Serialization.Abstractions;

/// <summary>Serializes simple (non-structured) options to directive lines using <see cref="EffectiveConfigWriteSemantics"/>.</summary>
public interface IEffectiveConfigDirectiveSerializer : IApplicationSingleton
{
    string SerializeSingle(string optionName, object? value);
    IReadOnlyList<string> SerializeMulti(string optionName, IReadOnlyList<string> values);
}
