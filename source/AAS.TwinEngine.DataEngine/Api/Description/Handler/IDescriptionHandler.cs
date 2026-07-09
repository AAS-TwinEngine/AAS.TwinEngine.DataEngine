using AAS.TwinEngine.DataEngine.Api.Description.Responses;

namespace AAS.TwinEngine.DataEngine.Api.Description.Handler;

public interface IDescriptionHandler
{
    ServiceDescriptionDto GetDescriptor(CancellationToken cancellationToken);
}
