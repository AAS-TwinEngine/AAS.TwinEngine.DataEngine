using System.Net;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.Shared.Results;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using NSwag.Annotations;

using ContentModifier = AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers.Content;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository;

[ApiController]
[Route("submodels")]
[ApiVersion(1)]
[OpenApiTags("Submodel Repository API")]
public class SubmodelRepositoryController(
    ILogger<SubmodelRepositoryController> logger,
    ISubmodelRepositoryHandler submodelRepositoryHandler)
    : ControllerBase
{
    /// <summary>
    /// Returns all Submodels.
    /// </summary>
    /// <param name="semanticId">The value of the semantic id reference (UTF8-BASE64-URL-encoded).</param>
    /// <param name="idShort">The Asset Administration Shell's IdShort.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata.</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <param name="extent">Determines the serialization of the returned content.</param>
    /// <response code="200">Returns the requested Submodels.</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="500">Internal Server Error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(SubmodelsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubmodelsDto>> GetAllSubmodelsAsync(
        [FromQuery] string? semanticId,
        [FromQuery] string? idShort,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level level = Level.deep,
        [FromQuery] Extent extent = Extent.withoutBlobValue)
    {
        logger.LogInformation("Get All Submodels");

        var request = new GetAllSubmodelsRequest(semanticId, idShort, limit, cursor, level, extent);

        var response = await submodelRepositoryHandler
            .GetAllSubmodels(request, cancellationToken)
            .ConfigureAwait(false);

        return Ok(response);
    }

    /// <summary>
    /// Returns a specific Submodel.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <param name="extent">Controls how blob values are serialized.</param>
    /// <response code="200">Requested Submodel</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}")]
    [ProducesResponseType(typeof(Submodel), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetSubmodelAsync(
        [FromRoute] string submodelIdentifier,
        CancellationToken cancellationToken,
        [FromQuery] Level level = Level.deep,
        [FromQuery] Extent extent = Extent.withoutBlobValue)
    {
        logger.LogInformation("Get Submodel");
        var request = new GetSubmodelRequest(submodelIdentifier, level, extent);
        var response = await submodelRepositoryHandler.GetSubmodel(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }

    /// <summary>
    /// Returns all SubmodelElements including their hierarchy.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue.</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <param name="extent">Determines the serialization of the returned content.</param>
    /// <response code="200">List of found submodel elements</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements")]
    [ProducesResponseType(typeof(SubmodelElementsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubmodelElementsDto>> GetAllSubmodelElementsAsync(
        [FromRoute] string submodelIdentifier,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level level = Level.deep,
        [FromQuery] Extent extent = Extent.withoutBlobValue)
    {
        logger.LogInformation("Get All Submodel Elements");
        var request = new GetAllSubmodelElementsRequest(submodelIdentifier, limit, cursor, level, extent);
        var response = await submodelRepositoryHandler.GetAllSubmodelElements(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>
    /// Returns a specific submodel element from the submodel at a specified path
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="idShortPath">The IdShort path to the submodel element (dot-separated)</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <param name="extent">Controls how blob values are serialized.</param>
    /// <response code="200">Requested submodel element</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/{idShortPath}")]
    [ProducesResponseType(typeof(ISubmodelElement), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetSubmodelElementAsync(
        [FromRoute] string submodelIdentifier,
        [FromRoute] string idShortPath,
        CancellationToken cancellationToken,
        [FromQuery] Level level = Level.deep,
        [FromQuery] Extent extent = Extent.withoutBlobValue)
    {
        logger.LogInformation("Get Submodel Element");
        var request = new GetSubmodelElementRequest(submodelIdentifier, idShortPath, level, extent);
        var response = await submodelRepositoryHandler.GetSubmodelElement(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }

    /// <summary>
    /// Downloads file content from a specific submodel element from the Submodel at a specified path.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="idShortPath">The IdShort path to the File SubmodelElement (dot-separated)</param>
    /// <response code="200">Requested File.</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/{idShortPath}/attachment")]
    [ProducesResponseType(typeof(FileResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetFileAttachmentAsync(
        [FromRoute] string submodelIdentifier,
        [FromRoute] string idShortPath,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get File Attachment");
        var request = new GetSubmodelElementRequest(submodelIdentifier, idShortPath);
        var attachment = await submodelRepositoryHandler.GetFileAttachment(request, cancellationToken).ConfigureAwait(false);
        return new FileContentStreamResult(attachment, ContentDispositionType.attachment);
    }

    /// <summary>
    /// Returns the metadata attributes of all Submodels.
    /// </summary>
    /// <param name="semanticId">The value of the semantic id reference (UTF8-BASE64-URL-encoded).</param>
    /// <param name="idShort">The Submodel's IdShort.</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="level">Not allowed for Content=Metadata; a request containing it is rejected.</param>
    /// <param name="extent">Only withoutBlobValue is allowed for Content=Metadata.</param>
    /// <response code="200">Requested Submodels in the Metadata representation.</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="500">Internal Server Error.</response>
    [HttpGet("$metadata")]
    [ProducesResponseType(typeof(PagedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<PagedContentDto>> GetAllSubmodelsMetadataAsync(
        [FromQuery] string? semanticId,
        [FromQuery] string? idShort,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level? level = null,
        [FromQuery] Extent? extent = null)
        => GetAllSubmodelsContentAsync(new GetAllSubmodelsRequest(semanticId, idShort, limit, cursor, level, extent), ContentModifier.Metadata, cancellationToken);

    /// <summary>
    /// Returns all Submodels in their ValueOnly representation.
    /// </summary>
    /// <param name="semanticId">The value of the semantic id reference (UTF8-BASE64-URL-encoded).</param>
    /// <param name="idShort">The Submodel's IdShort.</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <param name="extent">Determines the serialization of the returned content.</param>
    /// <response code="200">Requested Submodels in the ValueOnly representation.</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="500">Internal Server Error.</response>
    [HttpGet("$value")]
    [ProducesResponseType(typeof(PagedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<PagedContentDto>> GetAllSubmodelsValueOnlyAsync(
        [FromQuery] string? semanticId,
        [FromQuery] string? idShort,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level? level = null,
        [FromQuery] Extent? extent = null)
        => GetAllSubmodelsContentAsync(new GetAllSubmodelsRequest(semanticId, idShort, limit, cursor, level, extent), ContentModifier.Value, cancellationToken);

    /// <summary>
    /// Returns the References for all Submodels.
    /// </summary>
    /// <param name="semanticId">The value of the semantic id reference (UTF8-BASE64-URL-encoded).</param>
    /// <param name="idShort">The Submodel's IdShort.</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="level">Only core is allowed for Content=Reference.</param>
    /// <response code="200">References of the requested Submodels.</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="500">Internal Server Error.</response>
    [HttpGet("$reference")]
    [ProducesResponseType(typeof(PagedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<PagedContentDto>> GetAllSubmodelsReferenceAsync(
        [FromQuery] string? semanticId,
        [FromQuery] string? idShort,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level? level = null)
        => GetAllSubmodelsContentAsync(new GetAllSubmodelsRequest(semanticId, idShort, limit, cursor, level, null), ContentModifier.Reference, cancellationToken);

    /// <summary>
    /// Returns all Submodels in the Path notation.
    /// </summary>
    /// <param name="semanticId">The value of the semantic id reference (UTF8-BASE64-URL-encoded).</param>
    /// <param name="idShort">The Submodel's IdShort.</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <response code="200">IdShortPaths of the requested Submodels.</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="500">Internal Server Error.</response>
    [HttpGet("$path")]
    [ProducesResponseType(typeof(PagedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<PagedContentDto>> GetAllSubmodelsPathAsync(
        [FromQuery] string? semanticId,
        [FromQuery] string? idShort,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level? level = null)
        => GetAllSubmodelsContentAsync(new GetAllSubmodelsRequest(semanticId, idShort, limit, cursor, level, null), ContentModifier.Path, cancellationToken);

    /// <summary>
    /// Returns the metadata attributes of a specific Submodel.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="level">Not allowed for Content=Metadata; a request containing it is rejected.</param>
    /// <param name="extent">Only withoutBlobValue is allowed for Content=Metadata.</param>
    /// <response code="200">Requested Submodel in the Metadata representation</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/$metadata")]
    [ProducesResponseType(typeof(Submodel), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public Task<ActionResult<JsonNode>> GetSubmodelMetadataAsync(
        [FromRoute] string submodelIdentifier,
        CancellationToken cancellationToken,
        [FromQuery] Level? level = null,
        [FromQuery] Extent? extent = null)
        => GetSubmodelContentAsync(new GetSubmodelRequest(submodelIdentifier, level, extent), ContentModifier.Metadata, cancellationToken);

    /// <summary>
    /// Returns a specific Submodel in the ValueOnly representation.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <param name="extent">Controls how blob values are serialized.</param>
    /// <response code="200">Requested Submodel in the ValueOnly representation</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/$value")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public Task<ActionResult<JsonNode>> GetSubmodelValueOnlyAsync(
        [FromRoute] string submodelIdentifier,
        CancellationToken cancellationToken,
        [FromQuery] Level? level = null,
        [FromQuery] Extent? extent = null)
        => GetSubmodelContentAsync(new GetSubmodelRequest(submodelIdentifier, level, extent), ContentModifier.Value, cancellationToken);

    /// <summary>
    /// Returns the Reference of a specific Submodel.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="level">Only core is allowed for Content=Reference.</param>
    /// <response code="200">ModelReference of the requested Submodel</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/$reference")]
    [ProducesResponseType(typeof(Reference), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public Task<ActionResult<JsonNode>> GetSubmodelReferenceAsync(
        [FromRoute] string submodelIdentifier,
        CancellationToken cancellationToken,
        [FromQuery] Level? level = null)
        => GetSubmodelContentAsync(new GetSubmodelRequest(submodelIdentifier, level, null), ContentModifier.Reference, cancellationToken);

    /// <summary>
    /// Returns a specific Submodel in the Path notation.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <response code="200">IdShortPaths of the requested Submodel</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/$path")]
    [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public Task<ActionResult<JsonNode>> GetSubmodelPathAsync(
        [FromRoute] string submodelIdentifier,
        CancellationToken cancellationToken,
        [FromQuery] Level? level = null)
        => GetSubmodelContentAsync(new GetSubmodelRequest(submodelIdentifier, level, null), ContentModifier.Path, cancellationToken);

    /// <summary>
    /// Returns the metadata attributes of all submodel elements.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="level">Not allowed for Content=Metadata; a request containing it is rejected.</param>
    /// <param name="extent">Only withoutBlobValue is allowed for Content=Metadata.</param>
    /// <response code="200">Submodel elements in the Metadata representation</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/$metadata")]
    [ProducesResponseType(typeof(PagedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<PagedContentDto>> GetAllSubmodelElementsMetadataAsync(
        [FromRoute] string submodelIdentifier,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level? level = null,
        [FromQuery] Extent? extent = null)
        => GetAllSubmodelElementsContentAsync(new GetAllSubmodelElementsRequest(submodelIdentifier, limit, cursor, level, extent), ContentModifier.Metadata, cancellationToken);

    /// <summary>
    /// Returns all submodel elements in their ValueOnly representation.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <param name="extent">Determines the serialization of the returned content.</param>
    /// <response code="200">Submodel elements in the ValueOnly representation</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/$value")]
    [ProducesResponseType(typeof(PagedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<PagedContentDto>> GetAllSubmodelElementsValueOnlyAsync(
        [FromRoute] string submodelIdentifier,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level? level = null,
        [FromQuery] Extent? extent = null)
        => GetAllSubmodelElementsContentAsync(new GetAllSubmodelElementsRequest(submodelIdentifier, limit, cursor, level, extent), ContentModifier.Value, cancellationToken);

    /// <summary>
    /// Returns the References of all submodel elements.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="level">Only core is allowed for Content=Reference.</param>
    /// <response code="200">ModelReferences of the submodel elements</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/$reference")]
    [ProducesResponseType(typeof(PagedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<PagedContentDto>> GetAllSubmodelElementsReferenceAsync(
        [FromRoute] string submodelIdentifier,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level? level = null)
        => GetAllSubmodelElementsContentAsync(new GetAllSubmodelElementsRequest(submodelIdentifier, limit, cursor, level, null), ContentModifier.Reference, cancellationToken);

    /// <summary>
    /// Returns all submodel elements in the Path notation.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <response code="200">IdShortPaths of the submodel elements</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/$path")]
    [ProducesResponseType(typeof(PagedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<PagedContentDto>> GetAllSubmodelElementsPathAsync(
        [FromRoute] string submodelIdentifier,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit,
        [FromQuery] Level? level = null)
        => GetAllSubmodelElementsContentAsync(new GetAllSubmodelElementsRequest(submodelIdentifier, limit, cursor, level, null), ContentModifier.Path, cancellationToken);

    /// <summary>
    /// Returns the metadata attributes of a specific submodel element.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="idShortPath">The IdShort path to the submodel element (dot-separated)</param>
    /// <param name="level">Not allowed for Content=Metadata; a request containing it is rejected.</param>
    /// <param name="extent">Only withoutBlobValue is allowed for Content=Metadata.</param>
    /// <response code="200">Requested submodel element in the Metadata representation</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/{idShortPath}/$metadata")]
    [ProducesResponseType(typeof(ISubmodelElement), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public Task<IActionResult> GetSubmodelElementMetadataAsync(
        [FromRoute] string submodelIdentifier,
        [FromRoute] string idShortPath,
        CancellationToken cancellationToken,
        [FromQuery] Level? level = null,
        [FromQuery] Extent? extent = null)
        => GetSubmodelElementContentAsync(new GetSubmodelElementRequest(submodelIdentifier, idShortPath, level, extent), ContentModifier.Metadata, cancellationToken);

    /// <summary>
    /// Returns a specific submodel element in the ValueOnly representation.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="idShortPath">The IdShort path to the submodel element (dot-separated)</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <param name="extent">Controls how blob values are serialized.</param>
    /// <response code="200">Requested submodel element in the ValueOnly representation</response>
    /// <response code="400">Bad Request, e.g. the modifier is not defined for the element type.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/{idShortPath}/$value")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public Task<IActionResult> GetSubmodelElementValueOnlyAsync(
        [FromRoute] string submodelIdentifier,
        [FromRoute] string idShortPath,
        CancellationToken cancellationToken,
        [FromQuery] Level? level = null,
        [FromQuery] Extent? extent = null)
        => GetSubmodelElementContentAsync(new GetSubmodelElementRequest(submodelIdentifier, idShortPath, level, extent), ContentModifier.Value, cancellationToken);

    /// <summary>
    /// Returns the Reference of a specific submodel element.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="idShortPath">The IdShort path to the submodel element (dot-separated)</param>
    /// <param name="level">Only core is allowed for Content=Reference.</param>
    /// <response code="200">ModelReference of the requested submodel element</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/{idShortPath}/$reference")]
    [ProducesResponseType(typeof(Reference), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public Task<IActionResult> GetSubmodelElementReferenceAsync(
        [FromRoute] string submodelIdentifier,
        [FromRoute] string idShortPath,
        CancellationToken cancellationToken,
        [FromQuery] Level? level = null)
        => GetSubmodelElementContentAsync(new GetSubmodelElementRequest(submodelIdentifier, idShortPath, level, null), ContentModifier.Reference, cancellationToken);

    /// <summary>
    /// Returns a specific submodel element in the Path notation.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="idShortPath">The IdShort path to the submodel element (dot-separated)</param>
    /// <param name="level">Determines the structural depth of the returned content.</param>
    /// <response code="200">IdShortPaths of the requested submodel element and its children</response>
    /// <response code="400">Bad Request, e.g. the modifier is not defined for the element type.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/{idShortPath}/$path")]
    [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public Task<IActionResult> GetSubmodelElementPathAsync(
        [FromRoute] string submodelIdentifier,
        [FromRoute] string idShortPath,
        CancellationToken cancellationToken,
        [FromQuery] Level? level = null)
        => GetSubmodelElementContentAsync(new GetSubmodelElementRequest(submodelIdentifier, idShortPath, level, null), ContentModifier.Path, cancellationToken);

    private async Task<ActionResult<PagedContentDto>> GetAllSubmodelsContentAsync(GetAllSubmodelsRequest request, ContentModifier content, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get All Submodels with Content={Content}", content);
        var response = await submodelRepositoryHandler.GetAllSubmodelsContent(request, content, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    private async Task<ActionResult<JsonNode>> GetSubmodelContentAsync(GetSubmodelRequest request, ContentModifier content, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Submodel with Content={Content}", content);
        var response = await submodelRepositoryHandler.GetSubmodelContent(request, content, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    private async Task<ActionResult<PagedContentDto>> GetAllSubmodelElementsContentAsync(GetAllSubmodelElementsRequest request, ContentModifier content, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get All Submodel Elements with Content={Content}", content);
        var response = await submodelRepositoryHandler.GetAllSubmodelElementsContent(request, content, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    private async Task<IActionResult> GetSubmodelElementContentAsync(GetSubmodelElementRequest request, ContentModifier content, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Submodel Element with Content={Content}", content);
        var response = await submodelRepositoryHandler.GetSubmodelElementContent(request, content, cancellationToken).ConfigureAwait(false);

        // A ValueOnly property without value serializes to JSON null, which Ok() would turn into 204.
        return new JsonResult(response);
    }
}
