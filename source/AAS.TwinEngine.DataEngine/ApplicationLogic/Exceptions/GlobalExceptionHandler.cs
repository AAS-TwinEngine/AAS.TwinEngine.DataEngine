using System.Net;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Microsoft.AspNetCore.Diagnostics;

using NotImplementedException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base.NotImplementedException;
using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
                                                Exception exception,
                                                CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred.");

        var (statusCode, message) = GetErrorDetails(exception);

        var traceId = httpContext.TraceIdentifier;

        var response = new ServiceErrorResponse().Create((HttpStatusCode)statusCode,
                                                         title: message,
                                                         traceId: traceId);

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private static (int StatusCode, string Message) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, exception.Message),

            ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message),

            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),

            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message),

            TimeoutException => (StatusCodes.Status408RequestTimeout, exception.Message),

            ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, exception.Message),

            NotImplementedException => (StatusCodes.Status501NotImplemented, exception.Message),

            InternalServerException => (StatusCodes.Status500InternalServerError, exception.Message),

            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred while processing your request.")
        };
    }
}
