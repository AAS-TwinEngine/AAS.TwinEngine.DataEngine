using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_0;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;

public class AasRepositoryService(
    ILogger<AasRepositoryService> logger,
    IAasRepositoryTemplateService templateService,
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler) : IAasRepositoryService
{
    public async Task<Shells> GetShellsByFiltersAsync(
        IList<SpecificAssetIdFilter>? filters, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        try
        {
            var pluginManifests = pluginManifestConflictHandler.Manifests;
            IList<ShellDescriptorMetaData> shellDescriptorMetadataList;
            PagingMetaData pagingMetaData;

            if (filters is null || filters.Count == 0)
            {
                var metadata = await pluginDataHandler
                    .GetDataForAllShellDescriptorsAsync(limit, cursor, pluginManifests, cancellationToken)
                    .ConfigureAwait(false);

                shellDescriptorMetadataList = metadata.ShellDescriptors ?? [];
                pagingMetaData = metadata.PagingMetaData ?? new PagingMetaData();
            }
            else
            {
                var headerValue = SerializeFiltersHeader(filters);
                var metadata = await pluginDataHandler
                    .GetDataForShellDescriptorsByAssetIdsAsync(pluginManifests, headerValue, cancellationToken)
                    .ConfigureAwait(false);

                var allMetadata = metadata.ShellDescriptors?
                    .Where(m => !string.IsNullOrWhiteSpace(m.Id))
                    .ToList() ?? [];

                var (pagedItems, paged) = PagingExtensions.GetPagedResult(allMetadata, m => m.Id, limit, cursor);
                shellDescriptorMetadataList = pagedItems;
                pagingMetaData = paged;
            }

            var shells = new List<IAssetAdministrationShell>();
            foreach (var metadataItem in shellDescriptorMetadataList)
            {
                if (string.IsNullOrWhiteSpace(metadataItem.Id))
                {
                    continue;
                }

                try
                {
                    var shell = await templateService.GetShellTemplateAsync(metadataItem.Id, cancellationToken).ConfigureAwait(false);
                    FillShellFromMetadata(shell, metadataItem);
                    shells.Add(shell);
                }
                catch (Exception ex) when (ex is TemplateNotFoundException or InternalDataProcessingException)
                {
                    logger.LogWarning(ex, "Failed to build AAS for id {AasId}. Skipping.", metadataItem.Id);
                }
            }

            return new Shells
            {
                PagingMetaData = pagingMetaData,
                Result = shells
            };
        }
        catch (MultiPluginConflictException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new InternalDataProcessingException(ex);
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
        catch (UnauthorizedAccessException ex)
        {
            throw new ServiceUnAuthorizedException(ex);
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
        if (template is null)
        {
            throw new InvalidDependencyException(nameof(template));
        }

        if (pluginData is null)
        {
            throw new InvalidDependencyException(nameof(pluginData));
        }

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

    private static void FillShellFromMetadata(IAssetAdministrationShell shell, ShellDescriptorMetaData metadata)
    {
        shell.Id = metadata.Id;

        if (!string.IsNullOrWhiteSpace(metadata.IdShort))
        {
            shell.IdShort = metadata.IdShort;
        }

        shell.AssetInformation ??= new AssetInformation(AssetKind.Instance);
        shell.AssetInformation.GlobalAssetId = metadata.GlobalAssetId;

        if (metadata.SpecificAssetIds is not null)
        {
            shell.AssetInformation.SpecificAssetIds = [];
            foreach (var assetId in metadata.SpecificAssetIds)
            {
                shell.AssetInformation.SpecificAssetIds.Add(assetId);
            }
        }
    }

    private static string SerializeFiltersHeader(IList<SpecificAssetIdFilter> filters)
    {
        return JsonSerializer.Serialize(filters, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
}
