using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

public class ShellDescriptorService(
    ITemplateProvider templateProvider,
    IShellDescriptorDataHandler shellDescriptorDataHandler,
    IPluginDataHandler pluginDataHandler,
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
}
