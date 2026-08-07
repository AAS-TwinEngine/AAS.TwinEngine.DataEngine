using System.Text.Json;

using AAS.TwinEngine.DataEngine.Api.AasRepository.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;

public class AasRepositoryHandler(
    ILogger<AasRepositoryHandler> logger,
    IAasRepositoryService aasRepositoryService) : IAasRepositoryHandler
{
    public async Task<ShellsDto> GetShellsByAssetIdsAsync(
        string[]? assetIds, string? idShort, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        limit.ValidateLimit(logger);
        cursor?.ValidateCursor(logger);

        var assetIdsFilters = assetIds is not null && assetIds.Length > 0
            ? AssetIdHelper.DecodeAssetIds(assetIds, logger)
            : null;

        var filter = new ShellSearchFilter
        {
            SpecificAssetIds = assetIdsFilters,
            IdShort = idShort
        };

        var shells = await aasRepositoryService
            .GetShellsByFiltersAsync(filter, limit, cursor, cancellationToken)
            .ConfigureAwait(false);

        return shells.ToDto();
    }

    public Task<IAssetAdministrationShell> GetShellByIdAsync(GetShellRequest request, CancellationToken cancellationToken)
        => GetResourceByIdAsync(
            request?.AasIdentifier,
            "shell",
            id => aasRepositoryService.GetShellByIdAsync(id, cancellationToken)
        );

    public Task<IAssetInformation> GetAssetInformationByIdAsync(GetAssetInformationRequest request, CancellationToken cancellationToken)
        => GetResourceByIdAsync(
                request?.AasIdentifier,
                "asset information",
                id => aasRepositoryService.GetAssetInformationByIdAsync(id, cancellationToken)!
            );

    public Task<JsonElement> GetSubmodelRefByIdAsync(GetSubmodelRefRequest request, CancellationToken cancellationToken)
    {
        request?.Limit.ValidateLimit(logger);
        request?.Cursor?.ValidateCursor(logger);

        return GetResourceByIdAsync(
            request?.AasIdentifier,
            "submodel-ref",
            id => aasRepositoryService.GetSubmodelRefByIdAsync(id!, request?.Limit, request?.Cursor, cancellationToken)!,
            submodelRef => JsonSerializer.SerializeToElement(submodelRef.ToDto(), JsonSerializationOptions.SerializeToElementWithEnum)
        );
    }

    public Task<ISubmodel> GetSubmodelByAasIdAsync(GetSubmodelByAasRequest request, CancellationToken cancellationToken)
    {
        var queryOptions = new SubmodelQueryOptions(request.Level.ToString(), request.Extent.ToString());

        return GetResourceByIdAsync(
            request.AasIdentifier,
            request.SubmodelId,
            "submodel By AasID",
            async (aasId, submodelId) => await aasRepositoryService.GetSubmodelByAasIdAsync(aasId, submodelId, queryOptions, cancellationToken).ConfigureAwait(false));
    }

    public Task<SubmodelElementsDto> GetAllSubmodelElementsByAasIdAsync(
        GetAllSubmodelElementsByAasRequest request,
        CancellationToken cancellationToken)
    {
        request?.Limit.ValidateLimit(logger);
        request?.Cursor?.ValidateCursor(logger);

        var queryOptions = new SubmodelQueryOptions(request.Level.ToString(), request.Extent.ToString());

        return GetResourceByIdAsync(
            request.AasIdentifier,
            request.SubmodelId,
            "submodel elements By AasID",
            async (aasId, submodelId) => (await aasRepositoryService
                .GetAllSubmodelElementsByAasIdAsync(aasId, submodelId, queryOptions, request?.Limit, request?.Cursor, cancellationToken)
                .ConfigureAwait(false)).ToDto());
    }

    public Task<ISubmodelElement> GetSubmodelElementByAasIdAsync(
        GetSubmodelElementByAasRequest request,
        CancellationToken cancellationToken)
    {
        var decodedIdShortPath = Uri.UnescapeDataString(request?.IdShortPath ?? string.Empty);
        decodedIdShortPath.ValidateIdShortPath(nameof(request.IdShortPath), logger);

        return GetResourceByIdAsync(
            request!.AasIdentifier,
            request.SubmodelId,
            "submodel element By AasID",
            async (aasId, submodelId) => await aasRepositoryService.GetSubmodelElementByAasIdAsync(aasId, submodelId, request?.IdShortPath, cancellationToken).ConfigureAwait(false));
    }

    public Task<FileAttachmentResult> GetFileByPathByAasIdAsync(
        GetFileByPathByAasIdRequest request,
        CancellationToken cancellationToken)
    {
        var decodedIdShortPath = Uri.UnescapeDataString(request?.IdShortPath ?? string.Empty);
        decodedIdShortPath.ValidateIdShortPath(nameof(request.IdShortPath), logger);

        return GetResourceByIdAsync(
            request!.AasIdentifier,
            request.SubmodelIdentifier,
            "file By AasID",
            async (aasId, submodelId) => await aasRepositoryService.GetFileAttachmentByAasIdAsync(aasId, submodelId, decodedIdShortPath, cancellationToken).ConfigureAwait(false));
    }

    private Task<T> GetResourceByIdAsync<T>(
        string? encodedId,
        string resourceName,
        Func<string, Task<T?>> serviceFetchFunc)
        => GetResourceByIdAsync(encodedId, resourceName, serviceFetchFunc, model => model!);

    private async Task<TDto> GetResourceByIdAsync<TModel, TDto>(
        string? encodedId,
        string resourceName,
        Func<string, Task<TModel?>> fetchFunc,
        Func<TModel, TDto> mapFunc)
    {
        var decodedId = encodedId?.DecodeBase64Url(logger);
        logger.LogInformation("Start executing get request for {ResourceName}. Aas Identifier: {DecodedId}", resourceName, decodedId);

        var result = await fetchFunc(decodedId!).ConfigureAwait(false);
        ValidateResourceExists(result, resourceName, decodedId!);

        return mapFunc(result!);
    }

    private async Task<T> GetResourceByIdAsync<T>(
    string? encodedAasId,
    string? encodedSubmodelId,
    string resourceName,
     Func<string, string, Task<T?>> fetchFunc)
    {
        var decodedAasId = encodedAasId?.DecodeBase64Url(logger);
        var decodedSubmodelId = encodedSubmodelId?.DecodeBase64Url(logger);

        logger.LogInformation("Start executing get request for {ResourceName}. AAS: {AasId}, Submodel: {SubmodelId}", resourceName, decodedAasId, decodedSubmodelId);

        var result = await fetchFunc(decodedAasId, decodedSubmodelId)
            .ConfigureAwait(false);

        ValidateResourceExists(result, resourceName, decodedSubmodelId);

        return result;
    }

    private void ValidateResourceExists<T>(T? result, string resourceName, string decodedId)
    {
        if (result is null)
        {
            logger.LogWarning("{ResourceName} not found for Aas Identifier: {DecodedId}", resourceName, decodedId);
            throw new TemplateNotFoundException();
        }
    }
}
