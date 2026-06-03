using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.AasRepository.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;

public class AasRepositoryHandler(
    ILogger<AasRepositoryHandler> logger,
    IAasRepositoryService aasRepositoryService,
    IAssetIdSearchService assetIdSearchService,
    IAasRepositoryTemplateService templateService) : IAasRepositoryHandler
{
    public async Task<object> GetShellsByAssetIdsAsync(
        string[]? assetIds, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        limit.ValidateLimit(logger);
        cursor?.ValidateCursor(logger);

        if (assetIds is null || assetIds.Length == 0)
        {
            logger.LogError("assetIds query parameter is required.");
            throw new InvalidUserInputException();
        }

        var specificAssetIdFilters = AssetIdHelper.DecodeAssetIds(assetIds, logger);

        var (metadata, pagingMetaData) = await assetIdSearchService
            .GetShellMetadataByAssetIdsAsync(specificAssetIdFilters, limit, cursor, cancellationToken)
            .ConfigureAwait(false);

        var shells = new List<JsonObject>();
        foreach (var metadataItem in metadata)
        {
            try
            {
                var shell = await templateService.GetShellTemplateAsync(metadataItem.Id, cancellationToken).ConfigureAwait(false);

                AssetIdHelper.FillShellFromMetadata(shell, metadataItem);

                shells.Add(Jsonization.Serialize.ToJsonObject(shell));
            }
            catch (Exception ex) when (ex is TemplateNotFoundException or InternalDataProcessingException)
            {
                logger.LogWarning(ex, "Failed to build AAS for id {AasId}. Skipping.", metadataItem.Id);
            }
        }

        return new
        {
            paging_metadata = new { cursor = pagingMetaData.Cursor },
            result = shells
        };
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

    private void ValidateResourceExists<T>(T? result, string resourceName, string decodedId)
    {
        if (result is null)
        {
            logger.LogWarning("{ResourceName} not found for Aas Identifier: {DecodedId}", resourceName, decodedId);
            throw new TemplateNotFoundException();
        }
    }
}
