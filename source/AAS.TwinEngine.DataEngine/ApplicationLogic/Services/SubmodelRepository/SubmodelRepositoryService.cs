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

    // Default number of submodels per page when the client does not supply a limit.
    private const int DefaultPageSize = 100;
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

            submodelWithValues.Id = submodelId;

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
            var filteredTemplateId = await ResolveFilteredTemplateIdAsync(filter?.SemanticId, cancellationToken).ConfigureAwait(false);

            var (submodelIds, nextCursor) = await CollectSubmodelIdPageAsync(filter?.IdShort, limit, cursor, cancellationToken).ConfigureAwait(false);

            var submodels = await BuildSubmodelsAsync(submodelIds, filteredTemplateId, queryOptions, cancellationToken).ConfigureAwait(false);

            return new SubmodelList
            {
                PagingMetaData = new PagingMetaData { Cursor = nextCursor },
                Result = submodels
            };
        }).ConfigureAwait(false);
    }

    private async Task<string?> ResolveFilteredTemplateIdAsync(string? semanticId, CancellationToken cancellationToken)
    {
        if (semanticId is null)
        {
            return null;
        }

        return await submodelTemplateService.GetFilteredSubmodelTemplateIdAsync(semanticId, cancellationToken).ConfigureAwait(false)
               ?? throw new SubmodelNotFoundException();
    }

    /// <summary>
    /// Builds one page of submodel ids using the two-phase resume strategy: a partially consumed
    /// product is finished first (Phase 1), then forward pagination continues through the next
    /// products and plugin pages (Phase 2) until the page size is reached or the data is exhausted.
    /// </summary>
    private async Task<(IReadOnlyList<string> SubmodelIds, string? NextCursor)> CollectSubmodelIdPageAsync(
        string? idShort, int? limit, string? encodedCursor, CancellationToken cancellationToken)
    {
        var pageSize = limit ?? DefaultPageSize;
        var resumePoint = SubmodelPageCursor.TryDecode(encodedCursor);

        var collected = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // Plugin cursor that produced the batch currently being scanned (null = first plugin page).
        var pluginPageCursor = resumePoint?.PluginPageCursor;
        // Resume coordinates; only meaningful for the first scanned batch.
        var resumeAasId = resumePoint?.CurrentAasId;
        var resumeAfterSubmodelId = resumePoint?.LastSubmodelId;

        while (true)
        {
            var batch = await FetchShellBatchAsync(idShort, pageSize, pluginPageCursor, cancellationToken).ConfigureAwait(false);

            foreach (var aasId in SkipProductsBefore(batch.ShellIds, resumeAasId))
            {
                var submodelIds = await ExpandProductSubmodelIdsAsync(aasId, cancellationToken).ConfigureAwait(false);

                // On resume, drop the submodels that were already delivered from this product.
                if (resumeAfterSubmodelId is not null)
                {
                    submodelIds = SkipThrough(submodelIds, resumeAfterSubmodelId);
                    resumeAfterSubmodelId = null;
                }

                foreach (var submodelId in submodelIds)
                {
                    if (!seen.Add(submodelId))
                    {
                        continue;
                    }

                    collected.Add(submodelId);
                    if (collected.Count == pageSize)
                    {
                        var nextCursor = new SubmodelPageCursor(pluginPageCursor, aasId, submodelId).Encode();
                        return (collected, nextCursor);
                    }
                }
            }

            // The whole batch is consumed; resume state applies to the first batch only.
            resumeAasId = null;

            if (string.IsNullOrEmpty(batch.NextPluginCursor))
            {
                return (collected, null);
            }

            pluginPageCursor = batch.NextPluginCursor;
        }
    }

    private async Task<(List<string> ShellIds, string? NextPluginCursor)> FetchShellBatchAsync(
        string? idShort, int pageSize, string? pluginPageCursor, CancellationToken cancellationToken)
    {
        var shellSearchFilter = new ShellSearchFilter { IdShort = idShort };

        var metadata = await pluginDataHandler
            .GetDataForShellsByAssetIdsAsync(pluginManifestConflictHandler.Manifests, shellSearchFilter, pageSize, pluginPageCursor, cancellationToken)
            .ConfigureAwait(false);

        var shellIds = (metadata.ShellDescriptors ?? [])
            .Select(shell => shell.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        return (shellIds!, metadata.PagingMetaData?.Cursor);
    }

    private static IEnumerable<string> SkipProductsBefore(IEnumerable<string> shellIds, string? resumeAasId) =>
        resumeAasId is null ? shellIds : shellIds.SkipWhile(id => id != resumeAasId);

    private static List<string> SkipThrough(List<string> submodelIds, string lastDeliveredSubmodelId)
    {
        var index = submodelIds.IndexOf(lastDeliveredSubmodelId);
        return index < 0 ? submodelIds : [.. submodelIds.Skip(index + 1)];
    }

    private async Task<List<string>> ExpandProductSubmodelIdsAsync(string aasId, CancellationToken cancellationToken)
    {
        try
        {
            var references = await aasRepositoryTemplateService.GetSubmodelRefByIdAsync(aasId, cancellationToken).ConfigureAwait(false);

            return references
                .Select(reference => reference.Keys.FirstOrDefault()?.Value)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList()!;
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Could not retrieve submodel refs for shell {ShellId}. Skipping shell.", aasId);
            return [];
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

        return semanticIdHandler.FillOutTemplate(template, values);
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
