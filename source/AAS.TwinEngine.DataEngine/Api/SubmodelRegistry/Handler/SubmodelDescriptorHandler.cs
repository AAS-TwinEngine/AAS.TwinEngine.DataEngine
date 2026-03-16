using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Handler;

public class SubmodelDescriptorHandler(
    ILogger<SubmodelDescriptorHandler> logger,
    ISubmodelDescriptorService submodelDescriptorService) : ISubmodelDescriptorHandler
{
    public async Task<SubmodelDescriptorDto> GetSubmodelDescriptorById(GetSubmodelDescriptorRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start executing get request for submodel descriptor");

        var decodedId = request.SubmodelIdentifier?.DecodeBase64Url(logger);
        var submodelDescriptor = await submodelDescriptorService.GetSubmodelDescriptorByIdAsync(decodedId, cancellationToken).ConfigureAwait(false);

        if (submodelDescriptor != null)
        {
            var submodelDescriptorsDto = submodelDescriptor.ToDto();
            return submodelDescriptorsDto;
        }

        logger.LogWarning("Submodel Descriptor content not found. Submodel ID: {SubmodelId}", decodedId);
        throw new SubmodelDescriptorNotFoundException(decodedId);
    }
}

