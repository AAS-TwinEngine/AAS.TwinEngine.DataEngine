namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Authorization;

public class HeaderSanitizationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headerMappingService = context.RequestServices.GetRequiredService<IHeaderMappingService>();

        headerMappingService.ValidateIncomingHeaders(context);

        await _next(context).ConfigureAwait(false);
    }
}
