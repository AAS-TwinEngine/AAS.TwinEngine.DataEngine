using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;

public class AasRepositoryService(
    ILogger<AasRepositoryService> logger,
    IAasRepositoryTemplateService templateService,
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler) : IAasRepositoryService
{
    public async Task<Shells> GetShellsByFiltersAsync(IList<SpecificAssetId>? filters, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        try
        {
            var (metadata, pagingMetaData) = await GetShellMetadataAsync(filters, limit, cursor, cancellationToken).ConfigureAwait(false);

            var shells = await BuildShellsAsync(metadata, cancellationToken).ConfigureAwait(false);

            shells = [.. FilterByExternalSubjectId(shells, filters)];

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
        if (pluginData.SpecificAssetIds is not null)
        {
            foreach (var assetId in pluginData.SpecificAssetIds)
            {
                var existingAssetId = template.SpecificAssetIds?.FirstOrDefault(x => x.Name == assetId.Name);

                if (existingAssetId != null)
                {
                    existingAssetId.Value = assetId.Value;
                }
            }
        }
    }

    private void FillShellFromMetadata(IAssetAdministrationShell shell, ShellDescriptorMetaData metadata)
    {
        shell.Id = metadata.Id;

        if (!string.IsNullOrWhiteSpace(metadata.IdShort))
        {
            shell.IdShort = metadata.IdShort;
        }

        if (shell.AssetInformation is null)
        {
            logger.LogError("Shell template with id {AasId} has no AssetInformation. Cannot fill out metadata.", shell.Id);
            throw new TemplateNotValidException();
        }

        shell.AssetInformation.GlobalAssetId = metadata.GlobalAssetId;

        foreach (var assetId in metadata.SpecificAssetIds)
        {
            var existingAssetId = shell.AssetInformation.SpecificAssetIds?.FirstOrDefault(x => x.Name == assetId.Name);

            if (existingAssetId != null)
            {
                existingAssetId.Value = assetId.Value;
            }
        }
    }

    private async Task<(IList<ShellDescriptorMetaData>, PagingMetaData)> GetShellMetadataAsync(
    IList<SpecificAssetId>? filters,
    int? limit,
    string? cursor,
    CancellationToken cancellationToken)
    {
        return filters is null || filters.Count == 0
            ? await GetAllShellMetadataAsync(limit, cursor, cancellationToken).ConfigureAwait(false)
            : await GetFilteredShellMetadataAsync(filters, limit, cursor, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(IList<ShellDescriptorMetaData>, PagingMetaData)> GetAllShellMetadataAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var metadata = await pluginDataHandler
            .GetDataForAllShellDescriptorsAsync(limit, cursor, pluginManifestConflictHandler.Manifests, cancellationToken)
            .ConfigureAwait(false);

        return (
            metadata.ShellDescriptors ?? [],
            metadata.PagingMetaData ?? new PagingMetaData());
    }

    private async Task<(IList<ShellDescriptorMetaData>, PagingMetaData)> GetFilteredShellMetadataAsync(IList<SpecificAssetId> filters, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var metadata = await pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(pluginManifestConflictHandler.Manifests, filters, cancellationToken)
            .ConfigureAwait(false);

        var allMetadata = metadata.ShellDescriptors?
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .ToList() ?? [];

        var (pagedItems, pagingMetaData) = PagingExtensions.GetPagedResult(allMetadata, m => m.Id!, limit, cursor);

        return (pagedItems, pagingMetaData);
    }

    private async Task<List<IAssetAdministrationShell>> BuildShellsAsync(IEnumerable<ShellDescriptorMetaData> metadataItems, CancellationToken cancellationToken)
    {
        var shells = new List<IAssetAdministrationShell>();

        foreach (var metadata in metadataItems)
        {
            if (string.IsNullOrWhiteSpace(metadata.Id))
            {
                continue;
            }

            try
            {
                var shell = await templateService.GetShellTemplateAsync(metadata.Id, cancellationToken).ConfigureAwait(false);

                FillShellFromMetadata(shell, metadata);

                shells.Add(shell);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to build AAS for id {AasId}. Skipping.", metadata.Id);
            }
        }

        return shells;
    }

    private static IList<IAssetAdministrationShell> FilterByExternalSubjectId(IList<IAssetAdministrationShell> shells, IList<SpecificAssetId>? filters)
    {
        var filtersWithExternalId = filters?.Where(f => f.ExternalSubjectId is not null).ToList();

        if (filtersWithExternalId is null || filtersWithExternalId.Count == 0)
        {
            return shells;
        }

        return [.. shells
            .Where(shell =>
                shell.AssetInformation?.SpecificAssetIds?.Any(assetId =>
                    filtersWithExternalId.Any(filter =>
                        assetId.Name == filter.Name &&
                        assetId.Value == filter.Value &&
                        AreReferencesEqual(
                            assetId.ExternalSubjectId,
                            filter.ExternalSubjectId))) == true)];
    }

    private static bool AreReferencesEqual(IReference? first, IReference? second)
    {
        if (first is null || second is null)
        {
            return false;
        }

        if (first.Type != second.Type)
        {
            return false;
        }

        if (first.Keys.Count != second.Keys.Count)
        {
            return false;
        }

        return first.Keys.Zip(second.Keys)
            .All(pair =>
                pair.First.Type == pair.Second.Type &&
                pair.First.Value == pair.Second.Value);
    }
}
