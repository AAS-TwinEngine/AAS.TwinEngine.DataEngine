using AAS.TwinEngine.DataEngine.Api.AasRegistry.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.Handler;

public class ShellDescriptorHandler(
    ILogger<ShellDescriptorHandler> logger,
    IShellDescriptorService shellDescriptorService) : IShellDescriptorHandler
{
    public Task<ShellDescriptorsDto> GetAllShellDescriptors(GetShellDescriptorsRequest request, CancellationToken cancellationToken)
    {
        request?.Limit.ValidateLimit(logger);
        request?.Cursor?.ValidateCursor(logger);

        return GetResourceAsync(
            null,
            "shell descriptors",
            _ => shellDescriptorService.GetAllShellDescriptorsAsync(request.Limit, request.Cursor, cancellationToken),
            descriptors => descriptors.ToDto()
        );
    }

    public Task<ShellDescriptorDto> GetShellDescriptorById(GetShellDescriptorRequest request, CancellationToken cancellationToken)
        => GetResourceAsync(
            request?.AasIdentifier,
            "shell descriptor",
            id => shellDescriptorService.GetShellDescriptorByIdAsync(id!, cancellationToken),
            descriptor => descriptor.ToDto()
        );

    public Task<SubmodelDescriptorsDto> GetAllSubmodelDescriptorsByAasId(GetSubmodelDescriptorsByAasRequest request, CancellationToken cancellationToken)
    {
        request?.Limit.ValidateLimit(logger);
        request?.Cursor?.ValidateCursor(logger);

        return GetResourceAsync(
            request?.AasIdentifier,
            "submodel descriptors by AasId",
            aasId => shellDescriptorService.GetAllSubmodelDescriptorsByAasIdAsync(
                aasId,
                request.Limit,
                request.Cursor,
                cancellationToken),
            descriptors => descriptors.ToDto());
    }

    public Task<SubmodelDescriptorDto> GetSubmodelDescriptorByAasId(GetSubmodelDescriptorByAasRequest request, CancellationToken cancellationToken)
    {
        return GetResourceAsync(
            request?.AasIdentifier,
            request?.SubmodelIdentifier,
            "submodel descriptor",
            (aasId, submodelId) => shellDescriptorService.GetSubmodelDescriptorByAasIdAsync(aasId, submodelId, cancellationToken),
            descriptor => descriptor.ToDto());
    }

    private async Task<TDto> GetResourceAsync<TModel, TDto>(
        string? encodedAasId,
        string? encodedSubmodelId,
        string resourceName,
        Func<string, string, Task<TModel?>> fetchFunc,
        Func<TModel, TDto> mapFunc)
    {
        var decodedAasId = encodedAasId?.DecodeBase64Url(logger);
        var decodedSubmodelId = encodedSubmodelId?.DecodeBase64Url(logger);

        logger.LogInformation("Get {ResourceName} for AAS: {AasId}, Submodel: {SubmodelId}", resourceName, decodedAasId, decodedSubmodelId);

        var result = await fetchFunc(decodedAasId, decodedSubmodelId).ConfigureAwait(false);
        ValidateResourceExists(result, resourceName);
        return mapFunc(result);
    }

    private async Task<TDto> GetResourceAsync<TModel, TDto>(
        string? encodedId,
        string resourceName,
        Func<string?, Task<TModel?>> serviceFetchFunc,
        Func<TModel, TDto> mapFunc)
    {
        var decodedId = encodedId?.DecodeBase64Url(logger);
        LogRequestStart(resourceName, decodedId);

        var result = await serviceFetchFunc(decodedId).ConfigureAwait(false);
        ValidateResourceExists(result, resourceName);

        return mapFunc(result!);
    }

    private void LogRequestStart(string resourceName, string? decodedId)
    {
        if (resourceName is "shell descriptors")
        {
            logger.LogInformation("Start executing get request for {ResourceName}", resourceName);
        }
        else
        {
            logger.LogInformation("Start executing get request for {ResourceName} for AAS Identifier: {AasIdentifier}", resourceName, decodedId);
        }
    }

    private void ValidateResourceExists<TModel>(TModel? result, string resourceName)
    {
        if (result is null)
        {
            logger.LogError("{ResourceName} not found.", resourceName);
            throw new ShellDescriptorNotFoundException();
        }
    }
}
