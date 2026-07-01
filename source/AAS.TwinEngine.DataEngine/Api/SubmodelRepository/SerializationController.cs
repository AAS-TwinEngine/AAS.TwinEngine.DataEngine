using System.ComponentModel;
using System.Net;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using NSwag.Annotations;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository;

[ApiController]
[Route("serialization")]
[ApiVersion(1)]
[OpenApiTags("Serialization API")]
public class SerializationController(
    ILogger<SerializationController> logger,
    ISerializationHandler serializationHandler) : ControllerBase
{
    /// <summary>
    /// Returns an appropriate serialization based on the specified format(see SerializationFormat)
    /// </summary>
    /// <param name="aasIds">The Asset Administration Shells' unique ids (UTF8-BASE64-URL-encoded)</param>
    /// <param name="submodelIds">The Submodels' unique ids (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="includeConceptDescriptions">Include Concept Descriptions?</param>
    /// <response code="200">Requested serialization based on SerializationFormat</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("")]
    [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> SerializeAasxAsync([FromQuery] string[] aasIds,
                                                       [FromQuery] string[] submodelIds,
                                                       CancellationToken cancellationToken,
                                                       [FromQuery] bool includeConceptDescriptions = true)
    {
        logger.LogInformation("Start request to get aasx file");

        var request = new SerializeAasxRequest(aasIds, submodelIds, includeConceptDescriptions);

        var response = await serializationHandler.GetAasxFileAsync(request, cancellationToken).ConfigureAwait(false);

        return response;
    }
}
