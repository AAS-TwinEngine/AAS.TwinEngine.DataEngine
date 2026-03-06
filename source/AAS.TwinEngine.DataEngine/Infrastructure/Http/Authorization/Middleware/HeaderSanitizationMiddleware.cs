using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Middleware;

public class HeaderSanitizationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IRequestHeaderMapper requestHeaderMapper)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestHeaderMapper);

        requestHeaderMapper.ValidateIncomingHeaders(context);

        await _next(context).ConfigureAwait(false);
    }
}
