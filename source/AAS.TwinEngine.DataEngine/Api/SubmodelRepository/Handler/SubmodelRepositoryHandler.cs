using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;

public class SubmodelRepositoryHandler(
    ILogger<SubmodelRepositoryHandler> logger,
    ISubmodelRepositoryService submodelRepositoryService) : ISubmodelRepositoryHandler
{
    public Task<ISubmodel> GetSubmodel(GetSubmodelRequest request, CancellationToken cancellationToken)
    {
        var queryOptions = request?.Level is not null || request?.Extent is not null
            ? new SubmodelQueryOptions(request.Level?.ToString(), request.Extent?.ToString())
            : null;

        return GetResourceByIdAsync(
            request?.SubmodelId,
            "submodel",
            id => submodelRepositoryService.GetSubmodelAsync(id, queryOptions, cancellationToken)!
        );
    }

    public Task<ISubmodelElement> GetSubmodelElement(GetSubmodelElementRequest request, CancellationToken cancellationToken)
    {
        var decodedIdShortPath = Uri.UnescapeDataString(request?.IdShortPath ?? string.Empty);
        decodedIdShortPath.ValidateIdShortPath(nameof(request.IdShortPath), logger);

        return GetResourceByIdAsync(
            request?.SubmodelId,
            "submodel element",
            id => submodelRepositoryService.GetSubmodelElementAsync(id, decodedIdShortPath, cancellationToken)!
            );
    }

    public async Task<SubmodelsDto> GetAllSubmodels(GetAllSubmodelsRequest request, CancellationToken cancellationToken)
    {
        request?.Limit.ValidateLimit(logger);
        request?.Cursor?.ValidateCursor(logger);

        var filter = new SubmodelSearchFilter
        {
            SemanticId = request?.SemanticId,
            IdShort = request?.IdShort
        };

        var queryOptions = request?.Level is not null || request?.Extent is not null
            ? new SubmodelQueryOptions(request.Level?.ToString(), request.Extent?.ToString())
            : null;

        var result = await submodelRepositoryService
            .GetAllSubmodelsAsync(filter, queryOptions, request?.Limit, request?.Cursor, cancellationToken)
            .ConfigureAwait(false);

        return result.ToDto();
    }

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
