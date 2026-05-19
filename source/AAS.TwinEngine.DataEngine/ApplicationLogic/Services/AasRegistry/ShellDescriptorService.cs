using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

public class ShellDescriptorService(
    ITemplateProvider templateProvider,
    IShellTemplateMappingProvider shellTemplateMappingProvider,
    IShellDescriptorDataHandler shellDescriptorDataHandler,
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler,
    ILogger<ShellDescriptorService> logger) : IShellDescriptorService
{
    public async Task<ShellDescriptors?> GetAllShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        try
        {
            var pluginManifests = pluginManifestConflictHandler.Manifests;
            var metadata = await pluginDataHandler
                .GetDataForAllShellDescriptorsAsync(limit, cursor, pluginManifests, cancellationToken)
                .ConfigureAwait(false);

            var shellDescriptorMetadataList = metadata.ShellDescriptors ?? [];
            var shellDescriptors = new List<ShellDescriptor>(shellDescriptorMetadataList.Count);

            foreach (var shellDescriptorMetadata in shellDescriptorMetadataList)
            {
                var shellDescriptor = await TryBuildShellDescriptorAsync(shellDescriptorMetadata, cancellationToken).ConfigureAwait(false);
                if (shellDescriptor is not null)
                {
                    shellDescriptors.Add(shellDescriptor);
                }
            }

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
        if (string.IsNullOrWhiteSpace(shellDescriptorMetadata.Id))
        {
            logger.LogError("Failed to process ShellDescriptor. DescriptorId is missing. Continuing with remaining descriptors.");
            return null;
        }

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
