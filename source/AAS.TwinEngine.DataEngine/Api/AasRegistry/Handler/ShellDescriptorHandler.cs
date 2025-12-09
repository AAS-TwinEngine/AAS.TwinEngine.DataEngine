using AAS.TwinEngine.DataEngine.Api.AasRegistry.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;
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

        return GetShellDescriptorResourceAsync(
            null,
            "shell descriptors",
            _ => shellDescriptorService.GetAllShellDescriptorsAsync(request.Limit, request.Cursor, cancellationToken),
            descriptors => descriptors.ToDto()
        );
    }

    public Task<ShellDescriptorDto> GetShellDescriptorById(GetShellDescriptorRequest request, CancellationToken cancellationToken)
        => GetShellDescriptorResourceAsync(
            request?.AasIdentifier,
            "shell descriptor",
            id => shellDescriptorService.GetShellDescriptorByIdAsync(id!, cancellationToken),
            descriptor => descriptor.ToDto()
        );

    private async Task<TDto> GetShellDescriptorResourceAsync<TModel, TDto>(
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
        if (resourceName is "shell descriptor")
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
