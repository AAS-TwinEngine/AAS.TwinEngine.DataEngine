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

    private async Task<SubmodelPageResult> CollectSubmodelPageAsync(ShellSearchFilter shellSearchFilter, int pageSize, string? encodedCursor, CancellationToken cancellationToken)
    {
        var incomingCursor = SubmodelPaginationCursor.Decode(encodedCursor);
        var state = new PaginationState(incomingCursor, pageSize);
        var pluginCursor = state.TrackingAasId;

        while (state.CollectedIds.Count < pageSize)
        {
            var shellMetadata = await pluginDataHandler.GetDataForShellsByAssetIdsAsync(
                pluginManifestConflictHandler.Manifests, shellSearchFilter, pageSize, Base64UrlExtensions.EncodeBase64Url(pluginCursor), cancellationToken).ConfigureAwait(false);

            var shellDescriptors = shellMetadata.ShellDescriptors;
            if (shellDescriptors is null || shellDescriptors.Count == 0)
            {
                break;
            }

            var limitReached = await ProcessShellBatchAsync(shellDescriptors, pageSize, state, cancellationToken).ConfigureAwait(false);

            if (limitReached)
            {
                break;
            }

            if (shellMetadata.PagingMetaData?.Cursor is null)
            {
                break;
            }

            pluginCursor = state.TrackingAasId;
        }

        var nextCursor = state.CollectedIds.Count >= pageSize ? SubmodelPaginationCursor.Encode(state.LastCollectedSubmodelId, state.TrackingAasId) : null;

        return new SubmodelPageResult(state.CollectedIds, nextCursor);
    }

    private async Task<bool> ProcessShellBatchAsync(IReadOnlyList<ShellDescriptorMetaData> shellDescriptors, int pageSize, PaginationState state, CancellationToken cancellationToken)
    {
        var prefetchTasks = new Task<List<string>>[shellDescriptors.Count];
        using var semaphore = new SemaphoreSlim(_concurrentOperationsLimit, _concurrentOperationsLimit);

        for (var idx = 0; idx < shellDescriptors.Count; idx++)
        {
            var shellId = shellDescriptors[idx].Id;
            if (string.IsNullOrWhiteSpace(shellId))
            {
                prefetchTasks[idx] = Task.FromResult<List<string>>([]);
                continue;
            }

            prefetchTasks[idx] = PrefetchSubmodelIdsAsync(shellId, semaphore, cancellationToken);
        }

        var allSubmodelIds = await Task.WhenAll(prefetchTasks).ConfigureAwait(false);

        for (var idx = 0; idx < shellDescriptors.Count; idx++)
        {
            var shellId = shellDescriptors[idx].Id;
            if (string.IsNullOrWhiteSpace(shellId))
            {
                continue;
            }

            var submodelIds = allSubmodelIds[idx];

            if (submodelIds.Count == 0)
            {
                state.TrackingAasId = shellId;
                state.ResumeAfterSubmodelId = null;
                continue;
            }

            var startIndex = 0;

            if (state.ResumeAfterSubmodelId is not null)
            {
                startIndex = submodelIds.IndexOf(state.ResumeAfterSubmodelId) + 1;
                state.ResumeAfterSubmodelId = null;
            }

            for (var i = startIndex; i < submodelIds.Count; i++)
            {
                state.CollectedIds.Add(submodelIds[i]);
                state.LastCollectedSubmodelId = submodelIds[i];

                if (state.CollectedIds.Count >= pageSize)
                {
                    if (submodelIds[^1] == state.LastCollectedSubmodelId)
                    {
                        state.TrackingAasId = shellId;
                    }

                    return true;
                }
            }

            state.TrackingAasId = shellId;
        }

        return false;
    }

    private async Task<List<string>> PrefetchSubmodelIdsAsync(string shellId, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await GetSubmodelIdsForShellAsync(shellId, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    private async Task<List<string>> GetSubmodelIdsForShellAsync(string shellId, CancellationToken cancellationToken)
    {
        try
        {
            var references = await aasRepositoryTemplateService.GetSubmodelRefByIdAsync(shellId, cancellationToken).ConfigureAwait(false);

            return references.Select(r => r.Keys.FirstOrDefault()?.Value).Where(id => !string.IsNullOrWhiteSpace(id)).ToList()!;
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Could not retrieve submodel refs for shell {ShellId}. Skipping shell.", shellId);
            return [];
        }
    }

    private sealed record SubmodelPageResult(List<string> SubmodelIds, string? NextCursor);

    private sealed class PaginationState(SubmodelPaginationCursor? cursor, int capacity)
    {
        public List<string> CollectedIds { get; } = new(capacity);
        public string? TrackingAasId { get; set; } = cursor?.AasId;
        public string? LastCollectedSubmodelId { get; set; }
        public string? ResumeAfterSubmodelId { get; set; } = cursor?.SubmodelId;
    }

    private async Task<List<ISubmodel>> BuildSubmodelsAsync(List<string> submodelIds, string? filteredTemplateId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(_concurrentOperationsLimit, _concurrentOperationsLimit);
        var tasks = new Task<ISubmodel?>[submodelIds.Count];

        for (var i = 0; i < submodelIds.Count; i++)
        {
            tasks[i] = BuildSingleSubmodelAsync(submodelIds[i], filteredTemplateId, queryOptions, semaphore, cancellationToken);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var submodels = new List<ISubmodel>(results.Length);
        foreach (var result in results)
        {
            if (result is not null)
            {
                submodels.Add(result);
            }
        }

        return submodels;
    }

    private async Task<ISubmodel?> BuildSingleSubmodelAsync(string submodelId, string? filteredTemplateId, SubmodelQueryOptions? queryOptions, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var template = await submodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, filteredTemplateId, queryOptions, cancellationToken).ConfigureAwait(false);

            if (template is null)
            {
                return null;
            }

            return await BuildSubmodelWithValuesAsync(template, submodelId, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
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
