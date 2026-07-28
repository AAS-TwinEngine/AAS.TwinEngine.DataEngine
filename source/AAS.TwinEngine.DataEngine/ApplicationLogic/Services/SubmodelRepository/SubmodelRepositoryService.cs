using System.Globalization;

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
    private readonly long _maxFileSizeBytes = generalConfig.Value.SubmodelRepository.MaxFileSizeBytes;
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

    /// <summary>
    /// Wraps a network stream and disposes the owning <see cref="HttpResponseMessage"/> when the stream
    /// is disposed, preventing HTTP connection pool leaks.
    /// Logs when the stream is fully consumed, marking the end of the proxy-stream to the client.
    /// </summary>
    private sealed class ResponseBoundStream(
        Stream inner,
        HttpResponseMessage owner,
        ILogger logger,
        string fileName,
        string contentType) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                logger.LogInformation(
                    "[ClientStream] Stage E: Proxy-stream closed (sync) - all bytes delivered to client. FileName: {FileName}, ContentType: {ContentType}",
                    fileName, contentType);
                inner.Dispose();
                owner.Dispose();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            logger.LogInformation(
                "[ClientStream] Stage E: Proxy-stream closed (async) - all bytes delivered to client. FileName: {FileName}, ContentType: {ContentType}",
                fileName, contentType);
            await inner.DisposeAsync().ConfigureAwait(false);
            owner.Dispose();
            await base.DisposeAsync().ConfigureAwait(false);
        }
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
        logger.LogInformation(
            "[FileAttachment] Sequence started - SubmodelId: {SubmodelId}, IdShortPath: {IdShortPath}",
            submodelId,
            idShortPath);

        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            // Reuse existing element resolution to validate submodel + path and extract file metadata.
            logger.LogDebug("[FileAttachment] Stage 1: Resolving submodel element from template service");
            var element = await GetSubmodelElementAsync(submodelId, idShortPath, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("[FileAttachment] Stage 1 Complete: Element type resolved - {ElementType}", element?.GetType().Name ?? "null");

            if (element is not AasCore.Aas3_1.File fileElement)
            {
                logger.LogWarning(
                    "[FileAttachment] Stage 2 Failed: Element is not File type, got {ElementType}",
                    element?.GetType().Name ?? "null");
                throw new InvalidSubmodelElementTypeException(idShortPath);
            }
            logger.LogDebug("[FileAttachment] Stage 2 Complete: Validated File element type");

            var fileUrl = fileElement.Value;
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                logger.LogWarning(
                    "[FileAttachment] Stage 3 Failed: File URL is null or empty for {IdShortPath}",
                    idShortPath);
                throw new SubmodelElementNotFoundException(idShortPath);
            }

            var contentType = fileElement.ContentType ?? "application/octet-stream";
            var fileName = Path.GetFileName(fileUrl);
            logger.LogInformation(
                "[FileAttachment] Stage 3 Complete: File metadata extracted - FileName: {FileName}, ContentType: {ContentType}, FileUrl: {FileUrl}",
                fileName,
                contentType,
                fileUrl);

            // Stream the binary directly from the URL provided by the plugin in the File element value.
            logger.LogDebug("[FileAttachment] Stage 4: Creating HTTP client and initiating download");
            var httpClient = httpClientFactory.CreateClient();
            HttpResponseMessage response;
            try
            {
                logger.LogDebug("[FileAttachment] Stage 4a: Sending GET request to {FileUrl}", fileUrl);
                response = await httpClient
                    .GetAsync(new Uri(fileUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                logger.LogInformation(
                    "[FileAttachment] Stage 4b Complete: HTTP response received - StatusCode: {StatusCode}",
                    response.StatusCode);
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "[FileAttachment] Stage 4b Failed: HTTP request timeout/cancelled for {FileUrl}", fileUrl);
                throw new PluginNotAvailableException();
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "[FileAttachment] Stage 5 Failed: HTTP request unsuccessful - StatusCode: {StatusCode}, Reason: {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase);
                response.Dispose();
                throw new SubmodelElementNotFoundException(idShortPath);
            }

            // Enforce max file size using Content-Length if available (limit of 0 means disabled).
            var contentLength = response.Content.Headers.ContentLength;
            if (_maxFileSizeBytes > 0 && contentLength.HasValue && contentLength.Value > _maxFileSizeBytes)
            {
                response.Dispose();
                logger.LogWarning(
                    "[FileAttachment] Stage 5 Failed: File size {ContentLength} exceeds limit {MaxFileSizeBytes} for {IdShortPath}",
                    contentLength.Value,
                    _maxFileSizeBytes,
                    idShortPath);
                throw new FileSizeExceededException(idShortPath, contentLength.Value, _maxFileSizeBytes);
            }

            logger.LogDebug("[FileAttachment] Stage 5: Reading response stream");
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "[FileAttachment] Stage 6 Complete: Stream prepared for client delivery - FileName: {FileName}, ContentType: {ContentType}, ContentLength: {ContentLength}",
                fileName,
                contentType,
                contentLength?.ToString(CultureInfo.InvariantCulture) ?? "unknown");

            // Wrap the network stream so that the HttpResponseMessage is disposed together with the stream,
            // preventing HTTP connection pool leaks, and so that we can log stream completion.
            var boundStream = new ResponseBoundStream(stream, response, logger, fileName ?? "<unnamed>", contentType);
            return new FileAttachmentResult(boundStream, contentType, string.IsNullOrWhiteSpace(fileName) ? null : fileName);
        }).ConfigureAwait(false);
    }
}
