using Microsoft.Extensions.Primitives;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;

public static class RequestHeaderForwardingOptions
{
    public static readonly HttpRequestOptionsKey<IReadOnlyDictionary<string, StringValues>> IncomingHeaders =
        new("DataEngine.IncomingHeaders");
}

public interface IRequestHeaderMapper
{
    void ApplyMappings(HttpContext? httpContext, HttpRequestMessage outgoingRequest, string clientName);

    void ApplyMappings(IReadOnlyDictionary<string, StringValues>? incomingHeaders, HttpRequestMessage outgoingRequest, string clientName);

    void ValidateIncomingHeaders(HttpContext? httpContext);
}
