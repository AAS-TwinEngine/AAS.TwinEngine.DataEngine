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
    IPluginManifestConflictHandler pluginManifestConflictHandler) : IShellDescriptorService
{
    public async Task<ShellDescriptors?> GetAllShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        try
        {
            var pluginManifests = pluginManifestConflictHandler.Manifests;

            var metaData = await pluginDataHandler.GetDataForAllShellDescriptorsAsync(limit, cursor, pluginManifests, cancellationToken).ConfigureAwait(false);

            var shellDescriptors = new List<ShellDescriptor>(metaData.ShellDescriptors.Count);
            foreach (var shellDescriptorMetaData in metaData.ShellDescriptors)
            {
                var filledShellDescriptor = await BuildShellDescriptorAsync(shellDescriptorMetaData, cancellationToken).ConfigureAwait(false);
                shellDescriptors.Add(filledShellDescriptor);
            }

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

            var metaData = await pluginDataHandler.GetDataForShellDescriptorAsync(pluginManifests, id, cancellationToken).ConfigureAwait(false);

            return await BuildShellDescriptorAsync(metaData, cancellationToken).ConfigureAwait(false);
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
    }

    private async Task<ShellDescriptor> BuildShellDescriptorAsync(ShellDescriptorMetaData shellDescriptorMetaData, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shellDescriptorMetaData.Id))
        {
            throw new InternalDataProcessingException();
        }

        var templateId = shellTemplateMappingProvider.GetTemplateId(shellDescriptorMetaData.Id);
        var shellDescriptorTemplate = await templateProvider.GetShellDescriptorTemplateAsync(templateId!, cancellationToken).ConfigureAwait(false);

        return shellDescriptorDataHandler.FillOut(shellDescriptorTemplate, shellDescriptorMetaData);
    }
}
