namespace DnsmasqWebUI.Infrastructure.Services.Common.Serialization.Abstractions;

/// <summary>Generic contract to serialize a value to a string.</summary>
public interface IValueSerializer<in T>
{
    string SerializeValue(T value);
}
