using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

/// <summary>
/// Marker interface for options validators that are registered via assembly scanning.
/// Implement this (instead of <see cref="IValidateOptions{TOptions}"/> directly) so only intended validators are registered.
/// </summary>
public interface IApplicationOptionsValidator<TOptions> : IValidateOptions<TOptions> where TOptions : class
{
}
