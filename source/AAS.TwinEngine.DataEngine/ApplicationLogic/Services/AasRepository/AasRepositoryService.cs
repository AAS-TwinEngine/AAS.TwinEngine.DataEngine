using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;

public class AasRepositoryService(
    IAasRepositoryTemplateService templateService,
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler) : IAasRepositoryService
{
    public async Task<IAssetAdministrationShell?> GetShellByIdAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        var shellTemplate = await templateService.GetShellTemplateAsync(aasIdentifier, cancellationToken).ConfigureAwait(false);

        var assetInformation = await GetAssetInformationByIdAsync(aasIdentifier, cancellationToken).ConfigureAwait(false);

        shellTemplate.AssetInformation = assetInformation;
        shellTemplate.Id = aasIdentifier;

        return shellTemplate;
    }

    public async Task<IAssetInformation> GetAssetInformationByIdAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            var template = await templateService.GetAssetInformationTemplateAsync(aasIdentifier, cancellationToken).ConfigureAwait(false);

            var pluginManifests = pluginManifestConflictHandler.Manifests;

            var pluginData = await pluginDataHandler.GetDataForAssetInformationByIdAsync(pluginManifests, aasIdentifier, cancellationToken).ConfigureAwait(false);

            return FillOutAssetInformation(template, pluginData);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new AssetInformationNotFoundException(ex);
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (RequestTimeoutException ex)
        {
            throw new PluginNotAvailableException(ex);
        }
        catch (MultiPluginConflictException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (PluginMetaDataInvalidRequestException ex)
        {
            throw new InvalidUserInputException(ex);
        }
    }

    public async Task<SubmodelRef> GetSubmodelRefByIdAsync(string aasIdentifier, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var submodelRefs = await templateService.GetSubmodelRefByIdAsync(aasIdentifier, cancellationToken).ConfigureAwait(false);

        var (pagedItems, pagingMeta) = PagingExtensions.GetPagedResult(submodelRefs, s => s.Keys.FirstOrDefault()!.Value!, limit, cursor);

        return new SubmodelRef()
        {
            PagingMetaData = pagingMeta,
            Result = pagedItems
        };
    }

    private static IAssetInformation FillOutAssetInformation(IAssetInformation template, AssetData pluginData)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(pluginData);

        SetDefaultThumbnail(template, pluginData);
        SetGlobalAssetId(template, pluginData);
        SetSpecificAssetIds(template, pluginData);

        return template;
    }

    private static void SetDefaultThumbnail(IAssetInformation template, AssetData pluginData)
    {
        var thumbnail = pluginData.DefaultThumbnail;

        if (thumbnail is null || string.IsNullOrWhiteSpace(thumbnail.Path) || string.IsNullOrWhiteSpace(thumbnail.ContentType))
        {
            return;
        }

        template.DefaultThumbnail = new Resource(thumbnail.Path, thumbnail.ContentType);
    }

    private static void SetGlobalAssetId(IAssetInformation template, AssetData pluginData) => template.GlobalAssetId = pluginData.GlobalAssetId;

    private static void SetSpecificAssetIds(IAssetInformation template, AssetData pluginData)
    {
        template.SpecificAssetIds = [];

        if (pluginData.SpecificAssetIds is null)
        {
            return;
        }

        foreach (var assetId in pluginData.SpecificAssetIds)
        {
            template.SpecificAssetIds.Add(new SpecificAssetId(
                                                              name: assetId.Name ?? string.Empty,
                                                              value: assetId.Value ?? string.Empty
                                                             ));
        }
    }
}
