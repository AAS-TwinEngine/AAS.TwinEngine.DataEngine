using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
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
    IOptions<GeneralConfig> generalConfig,
    IOptions<TemplateManagementConfig> templateManagementConfig) : IShellDescriptorService
{
    private const string SubmodelUrlSegment = "submodel";

    private readonly int _concurrentOperationsLimit = templateManagementConfig.Value.AasTemplateRegistry.ConcurrentOperationsLimit;
    private readonly Uri _customerDomainUrl = generalConfig.Value.CustomerDomainUrl;
    private readonly Uri? _dataEngineRepositoryBaseUrl = generalConfig.Value.DataEngineRepositoryBaseUrl;

    public async Task<ShellDescriptors?> GetAllShellDescriptorsAsync(int? limit, string? cursor, AssetKind? assetKind, string? assetType, CancellationToken cancellationToken)
    {
        try
        {
            var pluginManifests = pluginManifestConflictHandler.Manifests;
            var metadata = await pluginDataHandler
                .GetDataForAllShellDescriptorsWithAssetFilterSupportAsync(limit, cursor, assetKind, assetType, pluginManifests, cancellationToken)
                .ConfigureAwait(false)
                ?? new ShellDescriptorsMetaData();

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

}
