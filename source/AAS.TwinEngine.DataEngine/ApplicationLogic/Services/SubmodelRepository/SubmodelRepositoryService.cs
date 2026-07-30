using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
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
    IHttpClientFactory httpClientFactory,
    IOptions<TemplateManagementConfig> templateManagementConfig,
    IOptions<GeneralConfig> generalConfig) : ISubmodelRepositoryService
{
    private readonly int _concurrentOperationsLimit = templateManagementConfig.Value.SubmodelTemplateRepository.ConcurrentOperationsLimit;
    private readonly int _fileAttachmentStreamingTimeoutSeconds = generalConfig.Value.FileAttachmentStreamingTimeoutSeconds;
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
            var shellSearchFilter = new ShellSearchFilter
            {
                IdShort = filter?.IdShort
            };

            var shellMetadata = await pluginDataHandler.GetDataForShellsByAssetIdsAsync(pluginManifestConflictHandler.Manifests, shellSearchFilter, cancellationToken).ConfigureAwait(false);
            var shellDescriptors = shellMetadata.ShellDescriptors ?? [];

            string? filteredTemplateId = null;
            if (filter?.SemanticId is not null)
            {
                filteredTemplateId = await submodelTemplateService.GetFilteredSubmodelTemplateIdAsync(filter.SemanticId, cancellationToken).ConfigureAwait(false);

                if (filteredTemplateId is null)
                {
                    throw new SubmodelNotFoundException();
                }
            }

            var distinctSubmodelIds = await GetDistinctSubmodelIdsAsync(shellDescriptors, cancellationToken).ConfigureAwait(false);

            var (pagedIds, pagingMetaData) = PagingExtensions.GetPagedResult(distinctSubmodelIds, id => id, limit, cursor);

            var submodels = await BuildSubmodelsAsync(pagedIds, filteredTemplateId, queryOptions, cancellationToken).ConfigureAwait(false);

            return new SubmodelList
            {
                PagingMetaData = pagingMetaData,
                Result = submodels
            };
        }).ConfigureAwait(false);
    }

    private async Task<List<string>> GetDistinctSubmodelIdsAsync(List<ShellDescriptorMetaData> shellDescriptors, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(_concurrentOperationsLimit, _concurrentOperationsLimit);
        var tasks = shellDescriptors.Where(shell => !string.IsNullOrWhiteSpace(shell.Id)).Select(async shell =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    return await aasRepositoryTemplateService.GetSubmodelRefByIdAsync(shell.Id!, cancellationToken).ConfigureAwait(false);
                }
                catch (ResourceNotFoundException ex)
                {
                    logger.LogWarning(ex, "Could not retrieve submodel refs for shell {ShellId}. Skipping shell.", shell.Id);
                    return [];
                }
                finally
                {
                    _ = semaphore.Release();
                }
            });

        var references = await Task.WhenAll(tasks).ConfigureAwait(false);

        return references
            .SelectMany(x => x)
            .Select(reference => reference.Keys.FirstOrDefault()?.Value)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList()!;
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

    public async Task<FileAttachmentResult> GetFileAttachmentAsync(string submodelId, string idShortPath, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var element = await GetSubmodelElementAsync(submodelId, idShortPath, cancellationToken).ConfigureAwait(false);

            if (element is not AasCore.Aas3_1.File fileElement)
            {
                throw new InvalidSubmodelElementTypeException(idShortPath);
            }

            var fileUrl = fileElement.Value;
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                throw new SubmodelElementNotFoundException(idShortPath);
            }

            if (!fileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !fileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException("File URL must start with http:// or https:// to be accessible.");
            }

            // timeoutCts is transferred to OwningStream so the timeout applies to the full streaming duration
            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_fileAttachmentStreamingTimeoutSeconds));

            var client = httpClientFactory.CreateClient();
            var upstreamResponse = await client.GetAsync(new Uri(fileUrl), HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token).ConfigureAwait(false);
            _ = upstreamResponse.EnsureSuccessStatusCode();

            var contentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = fileElement.IdShort;
            }

            var contentStream = await upstreamResponse.Content.ReadAsStreamAsync(timeoutCts.Token).ConfigureAwait(false);
            return new FileAttachmentResult(contentStream, contentType, fileName)
            {
                ResponseDisposables = [upstreamResponse, timeoutCts]
            };
        }).ConfigureAwait(false);
    }
}
