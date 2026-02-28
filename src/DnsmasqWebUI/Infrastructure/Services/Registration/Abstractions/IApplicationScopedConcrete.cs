namespace DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

/// <summary>
/// Marker for types that are registered as their own concrete type with scoped lifetime.
/// Used by assembly scanning (e.g. HTTP message handlers for HttpClient).
/// </summary>
public interface IApplicationScopedConcrete
{
}
