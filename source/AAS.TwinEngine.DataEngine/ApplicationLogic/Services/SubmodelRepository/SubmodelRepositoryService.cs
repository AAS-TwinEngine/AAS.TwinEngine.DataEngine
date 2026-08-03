using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Options;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public class SubmodelRepositoryService(
    ILogger<SubmodelRepositoryService> logger,
    ISubmodelTemplateService submodelTemplateService,
    ISemanticIdHandler semanticIdHandler,
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler,
    IAasRepositoryTemplateService aasRepositoryTemplateService,
    IOptions<TemplateManagementConfig> templateManagementConfig) : ISubmodelRepositoryService
{
    private readonly int _concurrentOperationsLimit = templateManagementConfig.Value.SubmodelTemplateRepository.ConcurrentOperationsLimit;
    public async Task<ISubmodel> GetSubmodelAsync(string submodelId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var submodelTemplate = await submodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, null, queryOptions, cancellationToken).ConfigureAwait(false);

            if (submodelTemplate is null)
            {
                throw new ResourceNotFoundException();
            }

            var submodelWithValues = await BuildSubmodelWithValuesAsync(submodelTemplate, submodelId, cancellationToken).ConfigureAwait(false);

            return submodelWithValues;
        }).ConfigureAwait(false);
    }

    public async Task<ISubmodelElement> GetSubmodelElementAsync(string submodelId, string idShortPath, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var reducedSubmodelTemplate = await submodelTemplateService.GetSubmodelTemplateAsync(submodelId, idShortPath, cancellationToken).ConfigureAwait(false);

            var submodelWithValues = await BuildSubmodelWithValuesAsync(reducedSubmodelTemplate, submodelId, cancellationToken).ConfigureAwait(false);

            return semanticIdHandler.Extract(submodelWithValues, idShortPath);
        }).ConfigureAwait(false);
    }


    public async Task<SubmodelList> GetAllSubmodelsAsync(SubmodelSearchFilter? filter, SubmodelQueryOptions? queryOptions, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            string? filteredTemplateId = null;
            if (filter?.SemanticId is not null)
            {
                filteredTemplateId = await submodelTemplateService.GetFilteredSubmodelTemplateIdAsync(filter.SemanticId, cancellationToken).ConfigureAwait(false);

                if (filteredTemplateId is null)
                {
                    throw new SubmodelNotFoundException();
                }
            }

            var shellSearchFilter = new ShellSearchFilter
            {
                IdShort = filter?.IdShort
            };

            var pageSize = limit ?? 100;
            var paginationResult = await CollectSubmodelPageAsync(shellSearchFilter, pageSize, cursor, cancellationToken).ConfigureAwait(false);

            var submodels = await BuildSubmodelsAsync(paginationResult.SubmodelIds, filteredTemplateId, queryOptions, cancellationToken).ConfigureAwait(false);

            return new SubmodelList
            {
                PagingMetaData = new PagingMetaData { Cursor = paginationResult.NextCursor },
                Result = submodels
            };
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Implements the two-field composite cursor pagination algorithm.
    /// Iterates through plugin shell batches, expanding submodel refs per AAS,
    /// and collects submodel IDs up to the requested page size.
    /// </summary>
    private async Task<SubmodelPageResult> CollectSubmodelPageAsync(
        ShellSearchFilter shellSearchFilter,
        int pageSize,
        string? encodedCursor,
        CancellationToken cancellationToken)
    {
        var incomingCursor = SubmodelPaginationCursor.Decode(encodedCursor);
        var state = new PaginationState(incomingCursor);
        var pluginCursor = state.TrackingAasId;

        while (state.CollectedIds.Count < pageSize)
        {
            var shellMetadata = await pluginDataHandler.GetDataForShellsByAssetIdsAsync(
                pluginManifestConflictHandler.Manifests, shellSearchFilter, pageSize, Base64UrlExtensions.EncodeBase64Url(pluginCursor), cancellationToken).ConfigureAwait(false);

            var shellDescriptors = shellMetadata.ShellDescriptors?
                .Where(s => !string.IsNullOrWhiteSpace(s.Id))
                .ToList() ?? [];

            if (shellDescriptors.Count == 0)
            {
                break;
            }

            var limitReached = await ProcessShellBatchAsync(shellDescriptors, pageSize, state, cancellationToken).ConfigureAwait(false);

            if (limitReached)
            {
                break;
            }

            // Batch exhausted without reaching limit — check if plugin has more data
            if (shellMetadata.PagingMetaData?.Cursor is null)
            {
                break;
            }

            pluginCursor = state.TrackingAasId;
        }

        var nextCursor = state.CollectedIds.Count >= pageSize
            ? SubmodelPaginationCursor.Encode(state.LastCollectedSubmodelId, state.TrackingAasId)
            : null;

        return new SubmodelPageResult(state.CollectedIds, nextCursor);
    }

    private async Task<bool> ProcessShellBatchAsync(
        List<ShellDescriptorMetaData> shellDescriptors,
        int pageSize,
        PaginationState state,
        CancellationToken cancellationToken)
    {
        foreach (var shell in shellDescriptors)
        {
            var submodelIds = await GetSubmodelIdsForShellAsync(shell.Id!, cancellationToken).ConfigureAwait(false);

            if (submodelIds.Count == 0)
            {
                state.TrackingAasId = shell.Id;
                state.IsFirstAasInResume = false;
                continue;
            }

            var startIndex = 0;

            // Skip-scan: only on the first AAS when resuming from a cursor
            if (state.IsFirstAasInResume && state.SkipToSubmodelId is not null)
            {
                startIndex = submodelIds.IndexOf(state.SkipToSubmodelId) + 1;
                state.IsFirstAasInResume = false;
                state.SkipToSubmodelId = null;
            }
            else
            {
                state.IsFirstAasInResume = false;
            }

            for (var i = startIndex; i < submodelIds.Count; i++)
            {
                state.CollectedIds.Add(submodelIds[i]);
                state.LastCollectedSubmodelId = submodelIds[i];

                if (state.CollectedIds.Count >= pageSize)
                {
                    if (state.CollectedIds.Contains(submodelIds.Last()))
                    {
                        state.TrackingAasId = shell.Id;
                    }

                    return true;
                }
            }

            state.TrackingAasId = shell.Id;
        }

        return false;
    }

    private async Task<List<string>> GetSubmodelIdsForShellAsync(string shellId, CancellationToken cancellationToken)
    {
        try
        {
            var references = await aasRepositoryTemplateService.GetSubmodelRefByIdAsync(shellId, cancellationToken).ConfigureAwait(false);

            return references
                .Select(r => r.Keys.FirstOrDefault()?.Value)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList()!;
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Could not retrieve submodel refs for shell {ShellId}. Skipping shell.", shellId);
            return [];
        }
    }

    private sealed record SubmodelPageResult(List<string> SubmodelIds, string? NextCursor);

    private sealed class PaginationState
    {
        public List<string> CollectedIds { get; } = [];
        public string? TrackingAasId { get; set; }
        public string? LastCollectedSubmodelId { get; set; }
        public string? SkipToSubmodelId { get; set; }
        public bool IsFirstAasInResume { get; set; }

        public PaginationState(SubmodelPaginationCursor? cursor)
        {
            TrackingAasId = cursor?.AasId;
            SkipToSubmodelId = cursor?.SubmodelId;
            IsFirstAasInResume = cursor is not null;
        }
    }

    private async Task<List<ISubmodel>> BuildSubmodelsAsync(IEnumerable<string> submodelIds, string? filteredTemplateId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(_concurrentOperationsLimit, _concurrentOperationsLimit);
        var tasks = submodelIds.Select(async submodelId =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var template = await submodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, filteredTemplateId, queryOptions, cancellationToken).ConfigureAwait(false);

                if (template is null)
                {
                    return null;
                }

                var submodel = await BuildSubmodelWithValuesAsync(template, submodelId, cancellationToken).ConfigureAwait(false);

                return submodel;
            }
            finally
            {
                _ = semaphore.Release();
            }
        });

        return [.. (await Task.WhenAll(tasks).ConfigureAwait(false)).OfType<ISubmodel>()];
    }

    public async Task<SubmodelElementsPage> GetAllSubmodelElementsAsync(string submodelId, SubmodelQueryOptions? queryOptions, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var submodelTemplate = await submodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, null, queryOptions, cancellationToken).ConfigureAwait(false);

            if (submodelTemplate is null)
            {
                throw new ResourceNotFoundException();
            }

            var submodelWithValues = await BuildSubmodelWithValuesAsync(submodelTemplate, submodelId, cancellationToken).ConfigureAwait(false);

            var allElements = submodelWithValues.SubmodelElements ?? [];

            var (pagedElements, pagingMetaData) = PagingExtensions.GetPagedResult(
                allElements,
                element => element.IdShort ?? string.Empty,
                limit,
                cursor);

            return new SubmodelElementsPage
            {
                PagingMetaData = pagingMetaData,
                Result = pagedElements
            };
        }).ConfigureAwait(false);
    }

    private async Task<ISubmodel> BuildSubmodelWithValuesAsync(ISubmodel template, string submodelId, CancellationToken cancellationToken)
    {
        var semanticIds = semanticIdHandler.Extract(template);

        var pluginManifests = pluginManifestConflictHandler.Manifests;

        var values = await pluginDataHandler.TryGetValuesAsync(pluginManifests, semanticIds, submodelId, cancellationToken).ConfigureAwait(false);

        var submodelWithValues = semanticIdHandler.FillOutTemplate(template, values);
        submodelWithValues.Id = submodelId;
        return submodelWithValues;
    }

    private static async Task<T> ExecuteWithExceptionHandlingAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new SubmodelNotFoundException(ex);
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
    }
}
