using DnsmasqWebUI.Infrastructure.Services.Common.Serialization.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers.Abstractions;

/// <summary>
/// Typed structured option handler: serialize/parse value and full directive line. Keyed by <see cref="IStructuredOptionValueHandler.OptionName"/>.
/// </summary>
public interface IStructuredOptionValueHandler<T> : IStructuredOptionValueHandler, IValueSerializer<T>, IValueParser<T>
{
    /// <summary>Full directive line (option=value) for the given value.</summary>
    string SerializeLine(T value);
}
