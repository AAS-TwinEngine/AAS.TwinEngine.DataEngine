using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;

public class AssetIdSearchService(
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler) : IAssetIdSearchService
{
    public async Task<(IList<string> AasIds, PagingMetaData PagingMetaData)> SearchShellsByAssetLinkAsync(
        IList<AssetLink> assetLinks, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var specificAssetIds = assetLinks.Select(link => new SpecificAssetIdFilter
        {
            Name = link.Name,
            Value = link.Value
        }).ToList();

        var headerValue = SerializeAssetIdsHeader(specificAssetIds);

        var metadata = await GetFilteredMetadataAsync(headerValue, cancellationToken).ConfigureAwait(false);

        var allIds = metadata.ShellDescriptors?
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => m.Id)
            .ToList() ?? [];

        var (pagedItems, pagingMetaData) = PagingExtensions.GetPagedResult(
            allIds, id => id, limit, cursor);

        return (pagedItems, pagingMetaData);
    }

    public async Task<(IList<ShellDescriptorMetaData> Metadata, PagingMetaData PagingMetaData)> GetShellMetadataByAssetIdsAsync(
        IList<SpecificAssetIdFilter> assetIds, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var headerValue = SerializeAssetIdsHeader(assetIds);

        var metadata = await GetFilteredMetadataAsync(headerValue, cancellationToken).ConfigureAwait(false);

        var allMetadata = metadata.ShellDescriptors?
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .ToList() ?? [];

        var (pagedItems, pagingMetaData) = PagingExtensions.GetPagedResult(
            allMetadata, m => m.Id, limit, cursor);

        return (pagedItems, pagingMetaData);
    }

    private async Task<ShellDescriptorsMetaData> GetFilteredMetadataAsync(string headerValue, CancellationToken cancellationToken)
    {
        try
        {
            var pluginManifests = pluginManifestConflictHandler.Manifests;

            return await pluginDataHandler
                .GetDataForShellDescriptorsByAssetIdsAsync(pluginManifests, headerValue, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MultiPluginConflictException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (UnauthorizedAccessException)
        {
            throw new ServiceUnAuthorizedException();
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (RequestTimeoutException ex)
        {
            throw new PluginNotAvailableException(ex);
        }
    }

    private static string SerializeAssetIdsHeader(IList<SpecificAssetIdFilter> assetIds)
    {
        return JsonSerializer.Serialize(assetIds, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
}
