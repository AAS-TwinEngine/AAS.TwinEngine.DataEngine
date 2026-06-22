using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

using AasCore.Aas3_1;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;

public class AssetIdSearchService(
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler) : IAssetIdSearchService
{
    public async Task<ShellsByAssetLink> SearchShellsByAssetLinkAsync(IList<AssetLink> assetLinks, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var specificAssetIds = assetLinks.Select(link => new SpecificAssetId
        (
            link.Name,
            link.Value
        )).ToList();

        var metadata = await GetFilteredMetadataAsync(specificAssetIds, cancellationToken).ConfigureAwait(false);

        var allIds = metadata.ShellDescriptors?
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => m.Id)
            .ToList() ?? [];

        var (pagedItems, pagingMetaData) = PagingExtensions.GetPagedResult(
            allIds, id => id, limit, cursor);

        return new ShellsByAssetLink
        {
            PagingMetaData = pagingMetaData,
            Result = pagedItems
        };
    }

    private async Task<ShellDescriptorsMetaData> GetFilteredMetadataAsync(List<SpecificAssetId> specificAssetIds, CancellationToken cancellationToken)
    {
        try
        {
            var pluginManifests = pluginManifestConflictHandler.Manifests;

            return await pluginDataHandler.GetDataForShellsByAssetIdsAsync(pluginManifests, specificAssetIds, cancellationToken).ConfigureAwait(false);
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
}
