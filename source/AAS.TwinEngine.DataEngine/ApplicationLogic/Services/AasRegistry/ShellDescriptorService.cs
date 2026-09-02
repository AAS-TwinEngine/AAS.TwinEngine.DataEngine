using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Options;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

public class ShellDescriptorService(
    ITemplateProvider templateProvider,
    IShellTemplateMappingProvider shellTemplateMappingProvider,
    IShellDescriptorDataHandler shellDescriptorDataHandler,
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler,
    ILogger<ShellDescriptorService> logger,
    IOptions<TemplateManagementConfig> templateManagementConfig,
    ISubmodelDescriptorService submodelDescriptorService,
    IAasRepositoryService aasRepositoryService) : IShellDescriptorService
    IOptions<GeneralConfig> generalConfig) : IShellDescriptorService
{
    private const int DefaultFallbackPluginPageSize = 100;
    private const int FallbackPluginPageSizeMultiplier = 10;
    private const int MaxFallbackPluginPageSize = 10_000;
    private const string SubmodelUrlSegment = "submodel";

    private readonly int _concurrentOperationsLimit = templateManagementConfig.Value.AasTemplateRegistry.ConcurrentOperationsLimit;
    private readonly Uri _customerDomainUrl = generalConfig.Value.CustomerDomainUrl;
    private readonly Uri? _dataEngineRepositoryBaseUrl = generalConfig.Value.DataEngineRepositoryBaseUrl;

    public async Task<ShellDescriptors?> GetAllShellDescriptorsAsync(int limit, string? cursor, AssetKind? assetKind, string? assetType, CancellationToken cancellationToken)
    {
        try
        {
            var pluginManifests = pluginManifestConflictHandler.Manifests;

            if (ShouldUseClientSideAssetKindTypeFallback(pluginManifests, assetKind, assetType))
            {
                return await GetAllShellDescriptorsWithClientSideAssetFilterAsync(limit, cursor, assetKind, assetType, pluginManifests, cancellationToken).ConfigureAwait(false);
            }

            var metadata = await pluginDataHandler
                .GetDataForAllShellDescriptorsAsync(limit, cursor, assetKind, assetType, pluginManifests, cancellationToken)
                .ConfigureAwait(false);

            var shellDescriptorMetadataList = metadata.ShellDescriptors ?? [];
            var shellDescriptors = await BuildShellDescriptorsInParallelAsync(shellDescriptorMetadataList, cancellationToken).ConfigureAwait(false);

            return new ShellDescriptors
            {
                PagingMetaData = metadata.PagingMetaData,
                Result = shellDescriptors
            };
        }
        catch (MultiPluginConflictException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new ShellDescriptorNotFoundException(ex);
        }
        catch (PluginMetaDataInvalidRequestException ex)
        {
            throw new InvalidUserInputException(ex);
        }
        catch (ValidationFailedException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (UnauthorizedAccessException)
        {
            throw new ServiceUnAuthorizedException();
        }
    }

    private async Task<ShellDescriptors> GetAllShellDescriptorsWithClientSideAssetFilterAsync(
        int? limit,
        string? cursor,
        AssetKind? assetKind,
        string? assetType,
        IReadOnlyList<PluginManifest> pluginManifests,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Falling back to client-side asset kind/type filtering for shell descriptors.");

        var decodedAssetType = string.IsNullOrWhiteSpace(assetType)
            ? null
            : assetType.DecodeBase64Url(logger);

        var collectedDescriptors = new List<ShellDescriptor>();
        var pluginLimit = limit is > 0 ? limit.Value : DefaultFallbackPluginPageSize;
        var pluginCursor = cursor;
        var pagingMetaData = new PagingMetaData();

        while (true)
        {
            var metadata = await pluginDataHandler
                .GetDataForAllShellDescriptorsAsync(pluginLimit, pluginCursor, null, null, pluginManifests, cancellationToken)
                .ConfigureAwait(false);

            var shellDescriptorMetadataList = metadata.ShellDescriptors ?? [];
            var shellDescriptors = await BuildShellDescriptorsInParallelAsync(shellDescriptorMetadataList, cancellationToken).ConfigureAwait(false);

            foreach (var descriptor in shellDescriptors.Where(descriptor => MatchesAssetKindTypeFilter(descriptor, assetKind, decodedAssetType)))
            {
                collectedDescriptors.Add(descriptor);

                if (limit.HasValue && collectedDescriptors.Count >= limit.Value)
                {
                    break;
                }
            }

            pagingMetaData = metadata.PagingMetaData ?? new PagingMetaData();

            if (limit.HasValue && collectedDescriptors.Count >= limit.Value)
            {
                break;
            }

            pluginCursor = metadata.PagingMetaData?.Cursor;
            if (string.IsNullOrWhiteSpace(pluginCursor))
            {
                break;
            }

            pluginLimit = Math.Min(pluginLimit * FallbackPluginPageSizeMultiplier, MaxFallbackPluginPageSize);
        }

        return new ShellDescriptors
        {
            PagingMetaData = pagingMetaData,
            Result = limit.HasValue ? [.. collectedDescriptors.Take(limit.Value)] : collectedDescriptors
        };
    }

    public async Task<ShellDescriptor?> GetShellDescriptorByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var pluginManifests = pluginManifestConflictHandler.Manifests;
            var metadata = await pluginDataHandler
                .GetDataForShellDescriptorAsync(pluginManifests, id, cancellationToken)
                .ConfigureAwait(false);

            var templateId = shellTemplateMappingProvider.GetTemplateId(metadata.Id)!;
            return await BuildShellDescriptorAsync(metadata, templateId, cancellationToken).ConfigureAwait(false);
        }
        catch (MultiPluginConflictException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new ShellDescriptorNotFoundException(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ServiceUnAuthorizedException(ex);
        }
        catch (PluginMetaDataInvalidRequestException ex)
        {
            throw new InvalidUserInputException(ex);
        }
        catch (ValidationFailedException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
    }

    public async Task<SubmodelDescriptor?> GetSubmodelDescriptorByAasIdAsync(string aasId, string submodelId, CancellationToken cancellationToken)
    {
        await aasRepositoryService.ValidateSubmodelBelongsToAasAsync(aasId, submodelId, cancellationToken).ConfigureAwait(false);

        return await submodelDescriptorService.GetSubmodelDescriptorByIdAsync(submodelId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubmodelDescriptors?> GetAllSubmodelDescriptorsByAasIdAsync(string aasId, int limit, string? cursor, CancellationToken cancellationToken)
    {
        var submodelRefs = await aasRepositoryService.GetSubmodelRefByIdAsync(aasId, null, null, cancellationToken).ConfigureAwait(false);

        var submodelIds = submodelRefs.Result?
            .SelectMany(reference => reference.Keys ?? [])
            .Select(key => key.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        var descriptors = (await Task.WhenAll(submodelIds.Select(submodelId =>
                                        submodelDescriptorService.GetSubmodelDescriptorByIdAsync(submodelId, cancellationToken))).ConfigureAwait(false))
                                        .OfType<SubmodelDescriptor>()
                                        .ToList();

        var (items, pagingMetaData) = PagingExtensions.GetPagedResult(descriptors, descriptor => descriptor.Id, limit, cursor);

        return new SubmodelDescriptors
        {
            Result = items,
            PagingMetaData = pagingMetaData
        };
    }

    private async Task<ShellDescriptor?> TryBuildShellDescriptorAsync(ShellDescriptorMetaData shellDescriptorMetadata, CancellationToken cancellationToken)
    {
        try
        {
            var templateId = shellTemplateMappingProvider.GetTemplateId(shellDescriptorMetadata.Id)!;
            return await BuildShellDescriptorAsync(shellDescriptorMetadata, templateId, cancellationToken).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogError(ex, "Failed to process ShellDescriptor. DescriptorId: {DescriptorId}. Reason: {Reason}. Continuing with remaining descriptors.", shellDescriptorMetadata.Id, ex.Message);
            return null;
        }
    }

    private async Task<List<ShellDescriptor>> BuildShellDescriptorsInParallelAsync(
        List<ShellDescriptorMetaData> metadataList,
        CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(_concurrentOperationsLimit, _concurrentOperationsLimit);
        var tasks = metadataList.Select(async metadata =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await TryBuildShellDescriptorAsync(metadata, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _ = semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return [.. results.OfType<ShellDescriptor>()];
    }

    private async Task<ShellDescriptor> BuildShellDescriptorAsync(
        ShellDescriptorMetaData shellDescriptorMetadata,
        string templateId,
        CancellationToken cancellationToken)
    {
        var shellDescriptorTemplate = await templateProvider
            .GetShellDescriptorTemplateAsync(templateId, cancellationToken)
            .ConfigureAwait(false);

        var descriptor = shellDescriptorDataHandler.FillOut(shellDescriptorTemplate, shellDescriptorMetadata);
        UpdateSubmodelDescriptors(descriptor, shellDescriptorMetadata.Id);
        return descriptor;
    }

    private void UpdateSubmodelDescriptors(ShellDescriptor descriptor, string shellId)
    {
        if (descriptor.SubmodelDescriptors is null || descriptor.SubmodelDescriptors.Count == 0)
        {
            return;
        }

        string? productId;
        try
        {
            productId = shellTemplateMappingProvider.GetProductIdFromRule(shellId);
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "No product ID found while updating submodel descriptors for shell {ShellId}", shellId);
            return;
        }

        if (string.IsNullOrWhiteSpace(productId))
        {
            return;
        }

        foreach (var submodelDescriptor in descriptor.SubmodelDescriptors)
        {
            if (string.IsNullOrWhiteSpace(submodelDescriptor.Id))
            {
                continue;
            }

            var updatedId = _customerDomainUrl + string.Join('/', SubmodelUrlSegment, productId, submodelDescriptor.Id);
            submodelDescriptor.Id = updatedId;

            if (_dataEngineRepositoryBaseUrl is null)
            {
                continue;
            }

            var encodedSubmodelId = updatedId.EncodeBase64Url(logger);
            var updatedHref = $"{_dataEngineRepositoryBaseUrl}{ApiPaths.Submodels}/{encodedSubmodelId}";

            foreach (var endpoint in submodelDescriptor.Endpoints ?? [])
            {
                endpoint.ProtocolInformation ??= new ProtocolInformationData();
                endpoint.ProtocolInformation.Href = updatedHref;
            }
        }
    }

    private static bool ShouldUseClientSideAssetKindTypeFallback(
        IReadOnlyList<PluginManifest> pluginManifests,
        AssetKind? assetKind,
        string? assetType)
    {
        var requiresFilter = assetKind.HasValue || !string.IsNullOrWhiteSpace(assetType);
        if (!requiresFilter)
        {
            return false;
        }

        var hasShellDescriptorPlugin = pluginManifests.Any(m => m.Capabilities.HasShellDescriptor);
        var hasFilterCapablePlugin = pluginManifests.Any(m => m.Capabilities.HasShellDescriptor && m.Capabilities.HasAssetKindTypeFilter == true);

        return hasShellDescriptorPlugin && !hasFilterCapablePlugin;
    }

    private static bool MatchesAssetKindTypeFilter(ShellDescriptor descriptor, AssetKind? assetKind, string? decodedAssetType)
    {
        if (assetKind.HasValue && descriptor.AssetKind != assetKind.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(decodedAssetType)
            && !string.Equals(descriptor.AssetType, decodedAssetType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
