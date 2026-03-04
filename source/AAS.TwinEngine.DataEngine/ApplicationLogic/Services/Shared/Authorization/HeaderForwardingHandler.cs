namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Authorization;

public sealed class HeaderForwardingHandler(
    IHttpContextAccessor httpContextAccessor,
    IHeaderMappingService headerMappingService,
    string clientName) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;

        headerMappingService.ApplyMappings(httpContext, request, clientName);

        return base.SendAsync(request, cancellationToken);
    }
}
