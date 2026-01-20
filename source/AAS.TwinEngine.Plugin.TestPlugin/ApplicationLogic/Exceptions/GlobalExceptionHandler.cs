using System.Net;

using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions.Responses;

using Microsoft.AspNetCore.Diagnostics;

namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
                                                Exception exception,
                                                CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred.");

        var statusCode = exception switch
        {
            BadRequestException => StatusCodes.Status400BadRequest,
            ForbiddenException => StatusCodes.Status403Forbidden,
            NotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            _ => StatusCodes.Status500InternalServerError
        };

        var traceId = httpContext.TraceIdentifier;

        var response = new ServiceErrorResponse().Create((HttpStatusCode)statusCode,
                                                         title: exception.Message,
                                                         traceId: traceId);

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
