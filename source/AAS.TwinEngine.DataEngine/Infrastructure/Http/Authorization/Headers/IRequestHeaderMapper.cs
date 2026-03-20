namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;

public interface IRequestHeaderMapper
{
    void ApplyMappings(HttpContext? httpContext, HttpRequestMessage outgoingRequest, string clientName);

    void ValidateIncomingHeaders(HttpContext? httpContext);
}
