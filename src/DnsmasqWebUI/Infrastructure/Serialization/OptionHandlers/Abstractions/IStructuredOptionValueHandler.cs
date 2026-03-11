using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers.Abstractions;

/// <summary>
/// Base contract for structured dnsmasq option value handlers. Keyed by <see cref="OptionName"/> (use <see cref="DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata.DnsmasqConfKeys"/>).
/// </summary>
public interface IStructuredOptionValueHandler : IApplicationMultiSingleton
{
    string OptionName { get; }
    Type ValueType { get; }
}
