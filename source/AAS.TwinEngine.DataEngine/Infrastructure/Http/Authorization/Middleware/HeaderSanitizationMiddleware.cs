using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Middleware;

public class HeaderSanitizationMiddleware(RequestDelegate next)
{
    private const string HealthCheckPath = "/healthz";
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IRequestHeaderMapper requestHeaderMapper)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestHeaderMapper);

        if (IsHealthCheckRequest(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        requestHeaderMapper.ValidateIncomingHeaders(context);

        await _next(context).ConfigureAwait(false);
    }

    private static bool IsHealthCheckRequest(HttpContext context) => context.Request.Path.StartsWithSegments(HealthCheckPath, StringComparison.OrdinalIgnoreCase);
}
