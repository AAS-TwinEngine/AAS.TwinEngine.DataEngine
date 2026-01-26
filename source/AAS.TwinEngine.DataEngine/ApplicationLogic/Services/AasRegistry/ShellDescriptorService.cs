using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

public class ShellDescriptorService(
    ITemplateProvider templateProvider,
    IShellDescriptorDataHandler shellDescriptorDataHandler,
    IPluginDataHandler pluginDataHandler,
    IAasRegistryProvider aasRegistryProvider,
    ILogger<ShellDescriptorService> logger,
    IPluginManifestConflictHandler pluginManifestConflictHandler) : IShellDescriptorService
{
    public async Task<ShellDescriptors?> GetAllShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        try
        {
            var shellDescriptorsTemplate = await templateProvider.GetShellDescriptorsTemplateAsync(cancellationToken).ConfigureAwait(false);

            var pluginManifests = pluginManifestConflictHandler.Manifests;

            var metaData = await pluginDataHandler.GetDataForAllShellDescriptorsAsync(limit, cursor, pluginManifests, cancellationToken).ConfigureAwait(false);

            var shellDescriptors = shellDescriptorDataHandler.FillOut(shellDescriptorsTemplate, metaData.ShellDescriptors);

            return new ShellDescriptors()
            {
                PagingMetaData = metaData.PagingMetaData,
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
    }

    public async Task<ShellDescriptor?> GetShellDescriptorByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var shellDescriptorTemplate = await templateProvider.GetShellDescriptorsTemplateAsync(cancellationToken).ConfigureAwait(false);

            var pluginManifests = pluginManifestConflictHandler.Manifests;

            var metaData = await pluginDataHandler.GetDataForShellDescriptorAsync(pluginManifests, id, cancellationToken).ConfigureAwait(false);

            return shellDescriptorDataHandler.FillOut(shellDescriptorTemplate, metaData);
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
    }

    public async Task SyncShellDescriptorsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var existingDescriptors = await aasRegistryProvider.GetAllAsync(cancellationToken).ConfigureAwait(false);
            if (existingDescriptors == null)
            {
                logger.LogError("AAS Registry returned null. Sync skipped.");
                return;
            }

            var pluginManifests = pluginManifestConflictHandler.Manifests;

            var pluginMetadata = await pluginDataHandler.GetDataForAllShellDescriptorsAsync(null, null, pluginManifests, cancellationToken).ConfigureAwait(false);
            if (pluginMetadata == null)
            {
                logger.LogError("Plugin metadata unavailable. Sync skipped.");
                return;
            }

            if (existingDescriptors.Any(d => string.IsNullOrWhiteSpace(d.Id)))
            {
                logger.LogError("One or more registry descriptors have missing IDs: {@Descriptors}", existingDescriptors);
                return;
            }

            if (pluginMetadata.ShellDescriptors.Any(m => string.IsNullOrWhiteSpace(m.Id)))
            {
                logger.LogError("One or more plugin metadata entries have missing IDs: {@Metadata}", pluginMetadata);
                return;
            }

            var existingDescriptorsMap = existingDescriptors.ToDictionary(d => d.Id!);
            var pluginMetadataMap = pluginMetadata.ShellDescriptors.ToDictionary(m => m.Id!);

            await CreateOrUpdateShellDescriptorsAsync(existingDescriptorsMap!, pluginMetadata.ShellDescriptors, cancellationToken).ConfigureAwait(false);
            await DeleteMissingShellDescriptorsAsync(existingDescriptors, pluginMetadataMap!, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during ShellDescriptor synchronization.");
        }
    }

    private async Task CreateOrUpdateShellDescriptorsAsync(
        Dictionary<string, ShellDescriptor> existingDescriptorsMap,
        IList<ShellDescriptorMetaData> pluginMetadata,
        CancellationToken cancellationToken)
    {
        foreach (var metadata in pluginMetadata)
        {
            try
            {
                if (existingDescriptorsMap.TryGetValue(metadata.Id!, out var existingDescriptor))
                {
                    var updatedDescriptor = shellDescriptorDataHandler.FillOut(existingDescriptor, metadata);
                    await aasRegistryProvider.PutAsync(existingDescriptor.Id!, updatedDescriptor, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var template = await templateProvider.GetShellDescriptorsTemplateAsync(cancellationToken).ConfigureAwait(false);
                    var newDescriptor = shellDescriptorDataHandler.FillOut(template, metadata);
                    await aasRegistryProvider.CreateAsync(newDescriptor, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error while processing descriptor with ID '{Id}'", metadata.Id);
                continue;
            }
        }
    }

    private async Task DeleteMissingShellDescriptorsAsync(
        List<ShellDescriptor> existingDescriptors,
        Dictionary<string, ShellDescriptorMetaData> pluginMetadataMap,
        CancellationToken cancellationToken)
    {
        var missingShellDescriptorsIds = existingDescriptors
                                        .Select(descriptor => descriptor.Id)
                                        .Where(id => !string.IsNullOrWhiteSpace(id) && !pluginMetadataMap.ContainsKey(id)).ToList();

        if (missingShellDescriptorsIds.Count == 0)
        {
            logger.LogInformation("No missing shell descriptors found to delete.");
            return;
        }
        foreach (var descriptorId in missingShellDescriptorsIds)
        {
            try
            {
                await aasRegistryProvider.DeleteByIdAsync(descriptorId!, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while deleting descriptor with ID '{Id}'", descriptorId);
                continue;
            }
        }
    }
}
