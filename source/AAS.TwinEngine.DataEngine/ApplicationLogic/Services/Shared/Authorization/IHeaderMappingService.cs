namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Authorization;

public interface IHeaderMappingService
{
    void ApplyMappings(HttpContext? httpContext, HttpRequestMessage outgoingRequest, string clientName);

    void ValidateIncomingHeaders(HttpContext? httpContext);
}
