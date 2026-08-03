using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Dependencies;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Streaming;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Options;

using Serilog.Core;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public class SubmodelRepositoryService(
    ILogger<SubmodelRepositoryService> logger,
    TemplateServices templateServices,
    ISemanticIdHandler semanticIdHandler,
    PluginServices pluginServices,
    IFileAttachmentStreamProvider fileAttachmentStreamProvider,
    IOptions<GeneralConfig> generalConfig) : ISubmodelRepositoryService
{
    private readonly int _concurrentOperationsLimit = templateServices.Config.SubmodelTemplateRepository.ConcurrentOperationsLimit;
    private readonly long _maxFileAttachmentSizeBytes = generalConfig.Value.MaxFileAttachmentSizeBytes;
    public async Task<ISubmodel> GetSubmodelAsync(string submodelId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var submodelTemplate = await templateServices.SubmodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, null, queryOptions, cancellationToken).ConfigureAwait(false);

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
            var reducedSubmodelTemplate = await templateServices.SubmodelTemplateService.GetSubmodelTemplateAsync(submodelId, idShortPath, cancellationToken).ConfigureAwait(false);

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

            var shellMetadata = await pluginServices.PluginDataHandler.GetDataForShellsByAssetIdsAsync(pluginServices.PluginManifestConflictHandler.Manifests, shellSearchFilter, cancellationToken).ConfigureAwait(false);
            var shellDescriptors = shellMetadata.ShellDescriptors ?? [];

            string? filteredTemplateId = null;
            if (filter?.SemanticId is not null)
            {
                filteredTemplateId = await templateServices.SubmodelTemplateService.GetFilteredSubmodelTemplateIdAsync(filter.SemanticId, cancellationToken).ConfigureAwait(false);

                if (filteredTemplateId is null)
                {
                    throw new SubmodelNotFoundException();
                }
            }

            var distinctSubmodelIds = await GetDistinctSubmodelIdsAsync(shellDescriptors, cancellationToken).ConfigureAwait(false);

            var (pagedIds, pagingMetaData) = PagingExtensions.GetPagedResult(distinctSubmodelIds, id => id, limit, cursor);

            var submodels = await BuildSubmodelsAsync(pagedIds.OfType<string>(), filteredTemplateId, queryOptions, cancellationToken).ConfigureAwait(false);

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
                    return await templateServices.AasRepositoryTemplateService.GetSubmodelRefByIdAsync(shell.Id, cancellationToken).ConfigureAwait(false);
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
                var template = await templateServices.SubmodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, filteredTemplateId, queryOptions, cancellationToken).ConfigureAwait(false);

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
            var submodelTemplate = await templateServices.SubmodelTemplateService.GetFilteredSubmodelTemplateAsync(submodelId, null, queryOptions, cancellationToken).ConfigureAwait(false);

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

        var pluginManifests = pluginServices.PluginManifestConflictHandler.Manifests;

        var values = await pluginServices.PluginDataHandler.TryGetValuesAsync(pluginManifests, semanticIds, submodelId, cancellationToken).ConfigureAwait(false);

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
            var fileElement = await GetFileElementAsync(submodelId, idShortPath, cancellationToken).ConfigureAwait(false);

            var fileUrl = GetValidatedFileUrl(fileElement, idShortPath);

            var upstreamResponse = await GetValidatedResponseAsync(fileUrl, cancellationToken).ConfigureAwait(false);
            var contentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            var fileName = GetFileName(fileElement, fileUrl);

            var upstreamStream = await fileAttachmentStreamProvider.ReadStreamAsync(upstreamResponse, cancellationToken).ConfigureAwait(false);
            var limitedStream = new MaxLengthStream(upstreamStream, _maxFileAttachmentSizeBytes);

            return new FileAttachmentResult(limitedStream, contentType, fileName)
            {
                ResponseDisposables = [upstreamResponse]
            };
        }).ConfigureAwait(false);
    }

    private async Task<AasCore.Aas3_1.File> GetFileElementAsync(string submodelId, string idShortPath, CancellationToken cancellationToken)
    {
        var element = await GetSubmodelElementAsync(submodelId, idShortPath, cancellationToken);

        return GetFileElement(element, idShortPath);
    }

    private AasCore.Aas3_1.File GetFileElement(ISubmodelElement element, string idShortPath)
    {
        if (element is AasCore.Aas3_1.File file)
        {
            return file;
        }
        logger.LogError("Submodel element at path {IdShortPath} is not of type File. Actual type: {ActualType}", idShortPath, element.GetType().Name);
        throw new InvalidSubmodelElementTypeException();
    }

    private string GetValidatedFileUrl(AasCore.Aas3_1.File fileElement, string idShortPath)
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
            throw new InvalidFileUrlException();
        }

        return fileUrl;
    }

    private async Task<HttpResponseMessage> GetValidatedResponseAsync(string fileUrl, CancellationToken cancellationToken)
    {
        var response = await fileAttachmentStreamProvider.GetResponseHeadersAsync(fileUrl, cancellationToken);

        _ = response.EnsureSuccessStatusCode();
        var actualContentLength = response.Content.Headers.ContentLength ?? 0;
        if (actualContentLength > _maxFileAttachmentSizeBytes)
        {
            throw new FileSizeExceededException();
        }

        return response;
    }

    private static string GetFileName(AasCore.Aas3_1.File fileElement, string fileUrl)
    {
        var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);

        return string.IsNullOrWhiteSpace(fileName)
            ? fileElement.IdShort ?? string.Empty
            : fileName;
    }
}
