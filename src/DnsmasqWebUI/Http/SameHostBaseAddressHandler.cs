using System.Net;

namespace DnsmasqWebUI.Http;

/// <summary>
/// DelegatingHandler that rewrites relative request URIs to the current request's scheme, host, and path base.
/// Used so Blazor/API callers can use relative paths (e.g. "/api/status") and hit the same host.
/// Resolved in the same scope as the code that requested the HttpClient (e.g. Blazor component),
/// so IHttpContextAccessor has the current request when available.
/// </summary>
public sealed class SameHostBaseAddressHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { IsAbsoluteUri: false })
        {
            var context = httpContextAccessor.HttpContext;
            var baseUri = context != null
                ? new Uri($"{context.Request.Scheme}://{context.Request.Host.Value}{context.Request.PathBase.Value ?? ""}")
                : new Uri("http://localhost", UriKind.Absolute);
            request.RequestUri = new Uri(baseUri, request.RequestUri);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
