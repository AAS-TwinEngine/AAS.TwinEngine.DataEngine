using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

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
{
    private readonly int _concurrentOperationsLimit = templateManagementConfig.Value.AasTemplateRegistry.ConcurrentOperationsLimit;
    public async Task<ShellDescriptors?> GetAllShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        try
        {
            var pluginManifests = pluginManifestConflictHandler.Manifests;
            var metadata = await pluginDataHandler
                .GetDataForAllShellDescriptorsAsync(limit, cursor, pluginManifests, cancellationToken)
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

    public async Task<SubmodelDescriptors?> GetAllSubmodelDescriptorsByAasIdAsync(string aasId, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var submodelRefs = await aasRepositoryService.GetSubmodelRefByIdAsync(aasId, null, null, cancellationToken).ConfigureAwait(false);

        var submodelIds = submodelRefs.Result?
            .SelectMany(reference => reference.Keys ?? [])
            .Select(key => key.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        var descriptors = (await Task.WhenAll(submodelIds.Select(submodelId =>
                                        GetSubmodelDescriptorByAasIdAsync(aasId, submodelId, cancellationToken))).ConfigureAwait(false))
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

        return shellDescriptorDataHandler.FillOut(shellDescriptorTemplate, shellDescriptorMetadata);
    }
}
