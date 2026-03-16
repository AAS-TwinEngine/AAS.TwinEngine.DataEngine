using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;

public class SubmodelRepositoryHandler(
    ILogger<SubmodelRepositoryHandler> logger,
    ISubmodelRepositoryService submodelRepositoryService) : ISubmodelRepositoryHandler
{
    public Task<ISubmodel> GetSubmodel(GetSubmodelRequest request, CancellationToken cancellationToken)
        => GetResourceByIdAsync(
            request?.SubmodelId,
            "submodel",
            id => submodelRepositoryService.GetSubmodelAsync(id, cancellationToken)!
        );

    public Task<ISubmodelElement> GetSubmodelElement(GetSubmodelElementRequest request, CancellationToken cancellationToken)
        => GetResourceByIdAsync(
            request?.SubmodelId,
            "submodel element",
            id => submodelRepositoryService.GetSubmodelElementAsync(id, request!.IdShortPath, cancellationToken)!
        );

    private async Task<T> GetResourceByIdAsync<T>(
        string? encodedId,
        string resourceName,
        Func<string, Task<T?>> serviceFetchFunc)
    {
        var decodedId = encodedId?.DecodeBase64Url(logger);
        logger.LogInformation("Start executing get request for {ResourceName}. ID: {DecodedId}", resourceName, decodedId);

        var result = await serviceFetchFunc(decodedId!).ConfigureAwait(false);
        ValidateResourceExists(result, resourceName, decodedId!);

        return result!;
    }

    private void ValidateResourceExists<T>(T? result, string resourceName, string decodedId)
    {
        if (result is not null)
        {
            return;
        }

        if (resourceName == "submodel")
        {
            logger.LogWarning("{ResourceName} not found for ID: {DecodedId}", resourceName, decodedId);
            throw new SubmodelElementNotFoundException(decodedId);
        }

        logger.LogWarning("{ResourceName} not found for ID: {DecodedId}", resourceName, decodedId);
        throw new SubmodelNotFoundException(decodedId);
    }
}
