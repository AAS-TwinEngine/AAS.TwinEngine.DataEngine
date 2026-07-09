using AAS.TwinEngine.DataEngine.Api.Description.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.Description.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Description;

namespace AAS.TwinEngine.DataEngine.Api.Description.Handler;

public class DescriptionHandler(
    ILogger<DescriptionHandler> logger,
    IDescriptionService descriptionService) : IDescriptionHandler
{
    public ServiceDescriptionDto GetDescriptor(CancellationToken cancellationToken)
    {
        logger.LogInformation("Start executing get request for descriptor");

        var descriptor = descriptionService.GetDescriptor(cancellationToken);

        return descriptor.ToDto();
    }
}
