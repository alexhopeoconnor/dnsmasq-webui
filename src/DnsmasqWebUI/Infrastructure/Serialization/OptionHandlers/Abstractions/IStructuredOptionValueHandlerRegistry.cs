using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers.Abstractions;

/// <summary>Lookup for structured option value handlers by option name (use <see cref="DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata.DnsmasqConfKeys"/>).</summary>
public interface IStructuredOptionValueHandlerRegistry : IApplicationSingleton
{
    bool IsStructured(string optionName);
    IStructuredOptionValueHandler? Get(string optionName);
    IStructuredOptionValueHandler<T>? Get<T>(string optionName);
    IStructuredOptionValueHandler<T> GetRequired<T>(string optionName);
}
