using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Options;

using File = AasCore.Aas3_1.File;
using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public class SubmodelRepositoryService(
    ILogger<SubmodelRepositoryService> logger,
    ISubmodelTemplateService submodelTemplateService,
    IAasRepositoryTemplateService aasRepositoryTemplateService,
    IOptions<TemplateManagementConfig> templateManagementConfig,
    ISemanticIdHandler semanticIdHandler,
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler,
    IFileContentProvider fileContentProvider,
    IOptions<GeneralConfig> generalConfig) : ISubmodelRepositoryService
{
    private readonly int _concurrentOperationsLimit = templateManagementConfig.Value.SubmodelTemplateRepository.ConcurrentOperationsLimit;
    private readonly long _maxFileAttachmentSizeBytes = generalConfig.Value.MaxFileAttachmentSizeBytes;

    public async Task<ISubmodel> GetSubmodelAsync(string submodelId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var submodelTemplate = await submodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, queryOptions, cancellationToken).ConfigureAwait(false);

            if (submodelTemplate is null)
            {
                throw new ResourceNotFoundException();
            }

            var submodelWithValues = await BuildSubmodelWithValuesAsync(submodelTemplate, submodelId, cancellationToken).ConfigureAwait(false);

            return submodelWithValues;
        }, ex => new SubmodelNotFoundException(ex)).ConfigureAwait(false);
    }

    public async Task<ISubmodelElement> GetSubmodelElementAsync(string submodelId, string idShortPath, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var reducedSubmodelTemplate = await submodelTemplateService.GetSubmodelTemplateAsync(submodelId, idShortPath, queryOptions, cancellationToken).ConfigureAwait(false);

            var submodelWithValues = await BuildSubmodelWithValuesAsync(reducedSubmodelTemplate, submodelId, cancellationToken).ConfigureAwait(false);

            return semanticIdHandler.Extract(submodelWithValues, idShortPath);
        }, ex => new SubmodelElementNotFoundException(ex)).ConfigureAwait(false);
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

            var pageSize = limit ?? GeneralConfig.DefaultPaginationLimit;
            var paginationResult = await CollectSubmodelPageAsync(shellSearchFilter, filteredTemplateId, pageSize, cursor, cancellationToken).ConfigureAwait(false);

            var submodels = await BuildSubmodelsAsync(paginationResult.SubmodelIds, queryOptions, cancellationToken).ConfigureAwait(false);

            return new SubmodelList
            {
                PagingMetaData = new PagingMetaData { Cursor = paginationResult.NextCursor },
                Result = submodels
            };
        }, ex => new SubmodelNotFoundException(ex)).ConfigureAwait(false);
    }

    private async Task<SubmodelPageResult> CollectSubmodelPageAsync(ShellSearchFilter shellSearchFilter, string? filteredTemplateId, int pageSize, string? encodedCursor, CancellationToken cancellationToken)
    {
        var incomingCursor = SubmodelPaginationCursor.Decode(encodedCursor);
        if (incomingCursor is null && !string.IsNullOrWhiteSpace(encodedCursor))
        {
            throw new InvalidUserInputException();
        }
        var state = new SubmodelPaginationState(incomingCursor, pageSize);
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

            var limitReached = await ProcessShellBatchAsync(shellDescriptors, filteredTemplateId, pageSize, state, cancellationToken).ConfigureAwait(false);

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

        return new SubmodelPageResult(state.CollectedIds, state.BuildNextCursor(pageSize));
    }

    private async Task<bool> ProcessShellBatchAsync(IReadOnlyList<ShellDescriptorMetaData> shellDescriptors, string? filteredTemplateId, int pageSize, SubmodelPaginationState state, CancellationToken cancellationToken)
    {
        var prefetchTasks = new Task<List<string?>>[shellDescriptors.Count];
        using var semaphore = new SemaphoreSlim(_concurrentOperationsLimit, _concurrentOperationsLimit);

        for (var idx = 0; idx < shellDescriptors.Count; idx++)
        {
            var shellId = shellDescriptors[idx].Id;
            if (string.IsNullOrWhiteSpace(shellId))
            {
                prefetchTasks[idx] = Task.FromResult<List<string?>>([]);
                continue;
            }

            prefetchTasks[idx] = PrefetchSubmodelIdsAsync(shellId, filteredTemplateId, semaphore, cancellationToken);
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

            if (state.CollectSubmodelIds(submodelIds, shellId, pageSize))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<List<string?>> PrefetchSubmodelIdsAsync(string shellId, string? filteredTemplateId, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await GetSubmodelIdsForShellAsync(shellId, filteredTemplateId, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    private async Task<List<string?>> GetSubmodelIdsForShellAsync(string shellId, string? filteredTemplateId, CancellationToken cancellationToken)
    {
        try
        {
            var references = await aasRepositoryTemplateService.GetSubmodelRefByIdAsync(shellId, cancellationToken).ConfigureAwait(false);

            var submodelIds = references.Select(reference => reference.Keys.FirstOrDefault()?.Value).Where(id => !string.IsNullOrWhiteSpace(id)).ToList();

            if (string.IsNullOrWhiteSpace(filteredTemplateId))
            {
                return submodelIds;
            }

            var validationTasks = submodelIds.Select(async id =>
                new
                {
                    Id = id,
                    IsValid = await submodelTemplateService.ValidateSemanticIdFilter(id, filteredTemplateId).ConfigureAwait(false)
                });

            var results = await Task.WhenAll(validationTasks).ConfigureAwait(false);

            return [.. results.Where(result => result.IsValid).Select(result => result.Id)];
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogWarning(ex, "Could not retrieve submodel refs for shell {ShellId}. Skipping shell.", shellId);

            return [];
        }
    }

    private async Task<List<ISubmodel>> BuildSubmodelsAsync(List<string> submodelIds, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(_concurrentOperationsLimit, _concurrentOperationsLimit);
        var tasks = new Task<ISubmodel?>[submodelIds.Count];

        for (var i = 0; i < submodelIds.Count; i++)
        {
            tasks[i] = BuildSingleSubmodelAsync(submodelIds[i], queryOptions, semaphore, cancellationToken);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var submodels = new List<ISubmodel>(results.Length);
        submodels.AddRange(results.Where(result => result is not null));

        return submodels;
    }

    private async Task<ISubmodel?> BuildSingleSubmodelAsync(string submodelId, SubmodelQueryOptions? queryOptions, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var template = await submodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, queryOptions, cancellationToken).ConfigureAwait(false);

            return await BuildSubmodelWithValuesAsync(template, submodelId, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public async Task<SubmodelElementsPage> GetAllSubmodelElementsAsync(string submodelId, SubmodelQueryOptions? queryOptions, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var submodelTemplate = await submodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, queryOptions, cancellationToken).ConfigureAwait(false);

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
        }, ex => new SubmodelElementNotFoundException(ex)).ConfigureAwait(false);
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

    private static async Task<T> ExecuteWithExceptionHandlingAsync<T>(
        Func<Task<T>> action,
        Func<ResourceNotFoundException, Exception> resourceNotFoundExceptionFactory)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (ResourceNotFoundException ex)
        {
            throw resourceNotFoundExceptionFactory(ex);
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

    public async Task<FileAttachmentResult> GetFileAttachmentAsync(string submodelId, string idShortPath, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var fileElement = await GetFileElementAsync(submodelId, idShortPath, cancellationToken).ConfigureAwait(false);

            var fileUrl = GetValidatedFileUrl(fileElement, idShortPath);

            var fileContent = await fileContentProvider.GetFileContentAsync(fileUrl, cancellationToken).ConfigureAwait(false);

            var contentType = !string.IsNullOrWhiteSpace(fileElement.ContentType) ? fileElement.ContentType : "application/octet-stream";

            var fileName = GetFileName(fileElement, fileUrl);

            return new FileAttachmentResult(fileContent.Content, contentType, fileName, _maxFileAttachmentSizeBytes)
            {
                Upstream = fileContent
            };
        }, ex => new SubmodelElementNotFoundException(ex)).ConfigureAwait(false);
    }

    private async Task<File> GetFileElementAsync(string submodelId, string idShortPath, CancellationToken cancellationToken)
    {
        var element = await GetSubmodelElementAsync(submodelId, idShortPath, null, cancellationToken);

        return GetFileElement(element, idShortPath);
    }

    private File GetFileElement(ISubmodelElement element, string idShortPath)
    {
        if (element is File file)
        {
            return file;
        }

        logger.LogError("Submodel element at path {IdShortPath} is not of type File. Actual type: {ActualType}", idShortPath, element.GetType().Name);
        throw new InvalidUserInputException();
    }

    private string GetValidatedFileUrl(File fileElement, string idShortPath)
    {
        var fileUrl = fileElement.Value;

        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            logger.LogError("File SubmodelElement at path {IdShortPath} has an empty or null value for the file URL.", idShortPath);
            throw new SubmodelElementNotFoundException(idShortPath);
        }

        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            logger.LogError("File SubmodelElement at path {IdShortPath} has an invalid URL: {FileUrl}", idShortPath, fileUrl);
            throw new InternalDataProcessingException();
        }

        return fileUrl;
    }

    private static string GetFileName(File fileElement, string fileUrl)
    {
        var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);

        return string.IsNullOrWhiteSpace(fileName) ? fileElement.IdShort ?? string.Empty : fileName;
    }
}
