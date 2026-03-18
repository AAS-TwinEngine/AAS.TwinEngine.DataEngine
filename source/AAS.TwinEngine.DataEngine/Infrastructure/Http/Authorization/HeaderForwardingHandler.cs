using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization;

public sealed class HeaderForwardingHandler(
    IHttpContextAccessor httpContextAccessor,
    IRequestHeaderMapper requestHeaderMapper,
    string clientName) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        requestHeaderMapper.ApplyMappings(httpContext, request, clientName);

        return base.SendAsync(request, cancellationToken);
    }
}
