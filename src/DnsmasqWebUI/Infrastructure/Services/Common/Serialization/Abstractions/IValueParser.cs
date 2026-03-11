namespace DnsmasqWebUI.Infrastructure.Services.Common.Serialization.Abstractions;

/// <summary>Generic contract to parse a value from text (e.g. for line-number context in errors).</summary>
public interface IValueParser<T>
{
    bool TryParseValue(string text, int lineNumber, out T? value);
}
