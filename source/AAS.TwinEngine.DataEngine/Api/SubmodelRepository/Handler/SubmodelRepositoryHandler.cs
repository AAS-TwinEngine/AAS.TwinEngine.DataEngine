using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;

public class SubmodelRepositoryHandler(
    ILogger<SubmodelRepositoryHandler> logger,
    ISubmodelRepositoryService submodelRepositoryService) : ISubmodelRepositoryHandler
{
    public Task<ISubmodel> GetSubmodel(GetSubmodelRequest request, CancellationToken cancellationToken)
    {
        var queryOptions = new SubmodelQueryOptions(request.Level.ToString(), request.Extent.ToString());

        return GetResourceByIdAsync(
            request.SubmodelId,
            "submodel",
            id => submodelRepositoryService.GetSubmodelAsync(id, queryOptions, cancellationToken)!);
    }

    public Task<ISubmodelElement> GetSubmodelElement(GetSubmodelElementRequest request, CancellationToken cancellationToken)
    {
        var decodedIdShortPath = DecodeAndValidateIdShortPath(request.IdShortPath);

        var queryOptions = new SubmodelQueryOptions(request.Level.ToString(), request.Extent.ToString());

        return GetResourceByIdAsync(
            request.SubmodelId,
            "submodel element",
            id => submodelRepositoryService.GetSubmodelElementAsync(id, decodedIdShortPath, queryOptions, cancellationToken));
    }

    public async Task<SubmodelsDto> GetAllSubmodels(GetAllSubmodelsRequest request, CancellationToken cancellationToken)
    {
        var result = await FetchAllSubmodelsAsync(request, cancellationToken).ConfigureAwait(false);

        return result.ToDto();
    }

    public async Task<SubmodelElementsDto> GetAllSubmodelElements(GetAllSubmodelElementsRequest request, CancellationToken cancellationToken)
    {
        var result = await FetchAllSubmodelElementsAsync(request, cancellationToken).ConfigureAwait(false);

        return result.ToDto();
    }

    public async Task<JsonNode> GetSubmodelContent(GetSubmodelRequest request, Content content, CancellationToken cancellationToken)
    {
        ContentModifierValidator.Validate(content, request.Level, request.Extent, logger);
        var effectiveRequest = request with
        {
            Level = ContentModifierValidator.ResolveLevel(content, request.Level),
            Extent = ContentModifierValidator.ResolveExtent(content, request.Extent)
        };

        var submodel = await GetSubmodel(effectiveRequest, cancellationToken).ConfigureAwait(false);

        return ContentSerializer.SerializeSubmodel(submodel, content);
    }

    public async Task<JsonNode?> GetSubmodelElementContent(GetSubmodelElementRequest request, Content content, CancellationToken cancellationToken)
    {
        ContentModifierValidator.Validate(content, request.Level, request.Extent, logger);
        var effectiveRequest = request with
        {
            Level = ContentModifierValidator.ResolveLevel(content, request.Level),
            Extent = ContentModifierValidator.ResolveExtent(content, request.Extent)
        };

        if (content == Content.Reference)
        {
            var decodedIdShortPath = DecodeAndValidateIdShortPath(effectiveRequest.IdShortPath);
            var queryOptions = new SubmodelQueryOptions(effectiveRequest.Level.ToString(), effectiveRequest.Extent.ToString());

            var reference = await GetResourceByIdAsync(
                effectiveRequest.SubmodelId,
                "submodel element",
                id => submodelRepositoryService.GetSubmodelElementReferenceAsync(id, decodedIdShortPath, queryOptions, cancellationToken)).ConfigureAwait(false);

            return ContentSerializer.SerializeReference(reference);
        }

        var element = await GetSubmodelElement(effectiveRequest, cancellationToken).ConfigureAwait(false);

        return ContentSerializer.SerializeSubmodelElement(element, content, Uri.UnescapeDataString(request.IdShortPath));
    }

    public async Task<PagedContentDto> GetAllSubmodelsContent(GetAllSubmodelsRequest request, Content content, CancellationToken cancellationToken)
    {
        ContentModifierValidator.Validate(content, request.Level, request.Extent, logger);
        var effectiveRequest = request with
        {
            Level = ContentModifierValidator.ResolveLevel(content, request.Level),
            Extent = ContentModifierValidator.ResolveExtent(content, request.Extent)
        };

        var result = await FetchAllSubmodelsAsync(effectiveRequest, cancellationToken).ConfigureAwait(false);

        return new PagedContentDto
        {
            PagingMetaData = new PagingMetaDataDto { Cursor = result.PagingMetaData?.Cursor },
            Result = ContentSerializer.SerializeSubmodels(result.Result ?? [], content)
        };
    }

    public async Task<PagedContentDto> GetAllSubmodelElementsContent(GetAllSubmodelElementsRequest request, Content content, CancellationToken cancellationToken)
    {
        ContentModifierValidator.Validate(content, request.Level, request.Extent, logger);
        var effectiveRequest = request with
        {
            Level = ContentModifierValidator.ResolveLevel(content, request.Level),
            Extent = ContentModifierValidator.ResolveExtent(content, request.Extent)
        };

        var result = await FetchAllSubmodelElementsAsync(effectiveRequest, cancellationToken).ConfigureAwait(false);
        var submodelId = request.SubmodelId?.DecodeBase64Url(logger) ?? string.Empty;

        return new PagedContentDto
        {
            PagingMetaData = new PagingMetaDataDto { Cursor = result.PagingMetaData?.Cursor },
            Result = ContentSerializer.SerializeSubmodelElements(result.Result ?? [], content, submodelId)
        };
    }

    private async Task<SubmodelList> FetchAllSubmodelsAsync(GetAllSubmodelsRequest request, CancellationToken cancellationToken)
    {
        request.Limit.ValidateLimit(logger);
        request.Cursor?.ValidateCursor(logger);

        var filter = new SubmodelSearchFilter
        {
            SemanticId = request.SemanticId,
            IdShort = request.IdShort
        };

        var queryOptions = new SubmodelQueryOptions(request.Level.ToString(), request.Extent.ToString());

        return await submodelRepositoryService.GetAllSubmodelsAsync(filter, queryOptions, request.Limit, request.Cursor, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SubmodelElementsPage> FetchAllSubmodelElementsAsync(GetAllSubmodelElementsRequest request, CancellationToken cancellationToken)
    {
        request.Limit.ValidateLimit(logger);
        request.Cursor?.ValidateCursor(logger);

        var queryOptions = new SubmodelQueryOptions(request.Level.ToString(), request.Extent.ToString());

        return await GetResourceByIdAsync(
            request.SubmodelId,
            "submodel",
            id => submodelRepositoryService.GetAllSubmodelElementsAsync(id, queryOptions, request.Limit, request.Cursor, cancellationToken)).ConfigureAwait(false);
    }

    private string DecodeAndValidateIdShortPath(string? idShortPath)
    {
        var decodedIdShortPath = Uri.UnescapeDataString(idShortPath ?? string.Empty);
        decodedIdShortPath.ValidateIdShortPath(nameof(GetSubmodelElementRequest.IdShortPath), logger);
        return decodedIdShortPath;
    }

    public async Task<FileAttachmentResult> GetFileAttachment(GetSubmodelElementRequest request, CancellationToken cancellationToken)
    {
        var decodedIdShortPath = Uri.UnescapeDataString(request.IdShortPath ?? string.Empty);
        decodedIdShortPath.ValidateIdShortPath(nameof(request.IdShortPath), logger);

        return await GetResourceByIdAsync(
            request.SubmodelId,
            "file attachment",
            id => submodelRepositoryService.GetFileAttachmentAsync(id, decodedIdShortPath, cancellationToken))
            .ConfigureAwait(false);
    }

    private async Task<T> GetResourceByIdAsync<T>(
        string? encodedId,
        string resourceName,
        Func<string, Task<T?>> serviceFetchFunc)
    {
        var decodedId = encodedId?.DecodeBase64Url(logger);
        logger.LogInformation("Start executing get request for {ResourceName}. ID: {DecodedId}", resourceName, decodedId);

        var result = await serviceFetchFunc(decodedId!).ConfigureAwait(false);
        ValidateResourceExists(result, resourceName, decodedId!);

        return result!;
    }

    private void ValidateResourceExists<T>(T? result, string resourceName, string decodedId)
    {
        if (result is not null)
        {
            return;
        }

        if (resourceName == "submodel")
        {
            logger.LogWarning("{ResourceName} not found for ID: {DecodedId}", resourceName, decodedId);
            throw new SubmodelElementNotFoundException(decodedId);
        }

        logger.LogWarning("{ResourceName} not found for ID: {DecodedId}", resourceName, decodedId);
        throw new SubmodelNotFoundException(decodedId);
    }
}
