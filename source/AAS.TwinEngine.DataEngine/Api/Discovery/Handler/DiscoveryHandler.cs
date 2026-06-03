using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

using AasCore.Aas3_0;

using Microsoft.AspNetCore.WebUtilities;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Handler;

public class DiscoveryHandler(
    ILogger<DiscoveryHandler> logger,
    IAssetIdSearchService assetIdSearchService,
    IAasRepositoryTemplateService templateService) : IDiscoveryHandler
{
    public async Task<ShellsByAssetLinkResponseDto> SearchShellsByAssetLinkAsync(
        AssetLink[] assetLinks, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        limit.ValidateLimit(logger);
        cursor?.ValidateCursor(logger);

        ValidateAssetLinks(assetLinks);

        var (aasIds, pagingMetaData) = await assetIdSearchService
            .SearchShellsByAssetLinkAsync(assetLinks, limit, cursor, cancellationToken)
            .ConfigureAwait(false);

        return new ShellsByAssetLinkResponseDto
        {
            PagingMetaData = new PagingMetaDataDto { Cursor = pagingMetaData.Cursor },
            Result = [.. aasIds]
        };
    }

    public async Task<object> GetShellsByAssetIdsAsync(
        string[]? assetIds, string? idShort, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        limit.ValidateLimit(logger);
        cursor?.ValidateCursor(logger);

        if (assetIds is null || assetIds.Length == 0)
        {
            logger.LogError("assetIds query parameter is required.");
            throw new InvalidUserInputException();
        }

        var specificAssetIdFilters = DecodeAssetIds(assetIds);

        var (metadata, pagingMetaData) = await assetIdSearchService
            .GetShellMetadataByAssetIdsAsync(specificAssetIdFilters, limit, cursor, cancellationToken)
            .ConfigureAwait(false);

        var shells = new List<JsonObject>();
        foreach (var metadataItem in metadata)
        {
            try
            {
                var shell = await templateService.GetShellTemplateAsync(metadataItem.Id, cancellationToken).ConfigureAwait(false);

                FillShellFromMetadata(shell, metadataItem);

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

    private IList<SpecificAssetIdFilter> DecodeAssetIds(string[] assetIds)
    {
        var result = new List<SpecificAssetIdFilter>();

        foreach (var encodedAssetId in assetIds)
        {
            if (string.IsNullOrWhiteSpace(encodedAssetId))
            {
                logger.LogError("Empty assetIds value encountered.");
                throw new InvalidUserInputException();
            }

            string decodedJson;
            try
            {
                var bytes = WebEncoders.Base64UrlDecode(encodedAssetId);
                decodedJson = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to decode base64url assetIds value: {Value}", encodedAssetId);
                throw new InvalidUserInputException();
            }

            SpecificAssetIdFilter? filter;
            try
            {
                filter = JsonSerializer.Deserialize<SpecificAssetIdFilter>(decodedJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to parse SpecificAssetId JSON: {Json}", decodedJson);
                throw new InvalidUserInputException();
            }

            if (filter is null || string.IsNullOrWhiteSpace(filter.Name) || string.IsNullOrWhiteSpace(filter.Value))
            {
                logger.LogError("Invalid SpecificAssetId: name and value are required.");
                throw new InvalidUserInputException();
            }

            if (filter.Name.Length > 64 || filter.Value.Length > 2048)
            {
                logger.LogError("SpecificAssetId name or value exceeds maximum length.");
                throw new InvalidUserInputException();
            }

            result.Add(filter);
        }

        return result;
    }

    private void ValidateAssetLinks(AssetLink[] assetLinks)
    {
        if (assetLinks is null || assetLinks.Length == 0)
        {
            logger.LogError("AssetLink array is empty or null.");
            throw new InvalidUserInputException();
        }

        foreach (var link in assetLinks)
        {
            if (string.IsNullOrWhiteSpace(link.Name) || string.IsNullOrWhiteSpace(link.Value))
            {
                logger.LogError("AssetLink name and value are required.");
                throw new InvalidUserInputException();
            }

            if (link.Name.Length > 64 || link.Value.Length > 2048)
            {
                logger.LogError("AssetLink name or value exceeds maximum length.");
                throw new InvalidUserInputException();
            }
        }
    }

    private static void FillShellFromMetadata(IAssetAdministrationShell shell, DomainModel.AasRegistry.ShellDescriptorMetaData metadata)
    {
        shell.Id = metadata.Id;

        if (!string.IsNullOrWhiteSpace(metadata.IdShort))
        {
            shell.IdShort = metadata.IdShort;
        }

        shell.AssetInformation ??= new AssetInformation(AssetKind.Instance);
        shell.AssetInformation.GlobalAssetId = metadata.GlobalAssetId;

        if (metadata.SpecificAssetIds is not null)
        {
            shell.AssetInformation.SpecificAssetIds = [];
            foreach (var assetId in metadata.SpecificAssetIds)
            {
                shell.AssetInformation.SpecificAssetIds.Add(assetId);
            }
        }
    }
}
