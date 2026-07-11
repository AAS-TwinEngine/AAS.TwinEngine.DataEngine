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
    public Task<SubmodelDescriptorsDto> GetAllSubmodelDescriptors(
        GetSubmodelDescriptorsRequest request,
        CancellationToken cancellationToken)
    {
        request?.Limit.ValidateLimit(logger);
        request?.Cursor?.ValidateCursor(logger);

        return GetSubmodelDescriptorResourceAsync(
            null,
            "submodel descriptors",
            _ => submodelDescriptorService.GetAllSubmodelDescriptorsAsync(
                request.Limit,
                request.Cursor,
                cancellationToken),
            descriptors => descriptors.ToDto());
    }

    public Task<SubmodelDescriptorDto> GetSubmodelDescriptorById(
        GetSubmodelDescriptorRequest request,
        CancellationToken cancellationToken)
        => GetSubmodelDescriptorResourceAsync(
            request?.SubmodelIdentifier,
            "submodel descriptor",
            id => submodelDescriptorService.GetSubmodelDescriptorByIdAsync(
                id,
                cancellationToken),
            descriptor => descriptor.ToDto());

    private async Task<TDto> GetSubmodelDescriptorResourceAsync<TModel, TDto>(
        string? encodedId,
        string resourceName,
        Func<string?, Task<TModel?>> serviceFetchFunc,
        Func<TModel, TDto> mapFunc)
    {
        var decodedId = encodedId?.DecodeBase64Url(logger);

        LogRequestStart(resourceName, decodedId);

        var result = await serviceFetchFunc(decodedId)
            .ConfigureAwait(false);

        ValidateResourceExists(result, resourceName, decodedId);

        return mapFunc(result!);
    }

    private void LogRequestStart(
        string resourceName,
        string? decodedId)
    {
        if (resourceName is "submodel descriptors")
        {
            logger.LogInformation("Start executing get request for {ResourceName}", resourceName);
        }
        else
        {
            logger.LogInformation("Start executing get request for {ResourceName} for Submodel Identifier: {SubmodelIdentifier}", resourceName, decodedId);
        }
    }

    private void ValidateResourceExists<TModel>(
        TModel? result,
        string resourceName,
        string? decodedId)
    {
        if (result is null)
        {
            logger.LogWarning("{ResourceName} not found. Submodel Identifier: {SubmodelIdentifier}", resourceName, decodedId);

            throw new SubmodelDescriptorNotFoundException();
        }
    }
}

