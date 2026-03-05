namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization;

public interface IRequestHeaderMapper
{
    void ApplyMappings(HttpContext? httpContext, HttpRequestMessage outgoingRequest, string clientName);

    void ValidateIncomingHeaders(HttpContext? httpContext);
}
