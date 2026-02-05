using System.Net;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Client;

/// <summary>
/// DelegatingHandler that rewrites relative request URIs to the current request's scheme, host, and path base.
/// Used so Blazor/API callers can use relative paths (e.g. "/api/status") and hit the same host.
/// Resolved in the same scope as the code that requested the HttpClient (e.g. Blazor component),
/// so IHttpContextAccessor has the current request when available.
/// </summary>
public sealed class SameHostBaseAddressHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler, IApplicationScopedConcrete
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context != null && request.RequestUri != null)
        {
            var baseUri = new Uri($"{context.Request.Scheme}://{context.Request.Host.Value}{context.Request.PathBase.Value ?? ""}");
            request.RequestUri = request.RequestUri.IsAbsoluteUri
                ? new Uri(baseUri, request.RequestUri.PathAndQuery)
                : new Uri(baseUri, request.RequestUri);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
