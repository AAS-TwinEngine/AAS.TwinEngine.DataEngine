using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
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

            var metaData = await pluginDataHandler.GetDataForAllShellDescriptorsAsync(limit, cursor, pluginManifests, cancellationToken).ConfigureAwait(false);
            var shellDescriptorMetaDataList = metaData.ShellDescriptors ?? [];

            var shellDescriptors = new List<ShellDescriptor>(shellDescriptorMetaDataList.Count);
            foreach (var shellDescriptorMetaData in shellDescriptorMetaDataList)
            {
                string? templateId = null;

                try
                {
                    templateId = ResolveTemplateId(shellDescriptorMetaData);
                    var filledShellDescriptor = await BuildShellDescriptorAsync(shellDescriptorMetaData, templateId, cancellationToken).ConfigureAwait(false);
                    shellDescriptors.Add(filledShellDescriptor);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ResourceNotFoundException ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to process ShellDescriptor. DescriptorId: {DescriptorId}, TemplateId: {TemplateId}. Continuing with remaining descriptors.",
                        shellDescriptorMetaData.Id,
                        templateId);
                }
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
            var templateId = ResolveTemplateId(metaData);

            return await BuildShellDescriptorAsync(metaData, templateId, cancellationToken).ConfigureAwait(false);
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

    private string ResolveTemplateId(ShellDescriptorMetaData shellDescriptorMetaData)
    {
        if (string.IsNullOrWhiteSpace(shellDescriptorMetaData.Id))
        {
            throw new InternalDataProcessingException();
        }

        return shellTemplateMappingProvider.GetTemplateId(shellDescriptorMetaData.Id)!;
    }

    private async Task<ShellDescriptor> BuildShellDescriptorAsync(ShellDescriptorMetaData shellDescriptorMetaData, string templateId, CancellationToken cancellationToken)
    {
        var shellDescriptorTemplate = await templateProvider.GetShellDescriptorTemplateAsync(templateId, cancellationToken).ConfigureAwait(false);

        return shellDescriptorDataHandler.FillOut(shellDescriptorTemplate, shellDescriptorMetaData);
    }
}
