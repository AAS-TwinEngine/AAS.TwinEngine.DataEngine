using System.Net;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository;

[ApiController]
[Route("serialization")]
[ApiVersion(1)]
public class SerializationController(
    ILogger<SerializationController> logger,
    ISerializationHandler serializationHandler) : ControllerBase
{
    /// <summary>
    /// Exports selected shells and submodels as an AASX package.
    /// </summary>
    /// <remarks>
    /// Route behavior is intentionally unchanged in this release for backward compatibility.
    /// Example identifiers are expected in base64url format.
    /// </remarks>
    /// <param name="aasIds">Base64url encoded AAS identifiers to include in the export.</param>
    /// <param name="submodelIds">Base64url encoded submodel identifiers to include in the export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="includeConceptDescriptions">If true, related concept descriptions are added to the package. Default: true.</param>
    /// <response code="200">AASX package stream was returned.</response>
    /// <response code="400">Input parameters are invalid.</response>
    /// <response code="404">One or more referenced entities were not found.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet("")]
    [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> SerializeAasxAsync([FromQuery, Description("Base64url encoded AAS identifiers.")] string[] aasIds,
                                                       [FromQuery, Description("Base64url encoded submodel identifiers.")] string[] submodelIds,
                                                       CancellationToken cancellationToken,
                                                       [FromQuery, Description("Include related concept descriptions in the exported AASX package.")] bool includeConceptDescriptions = true)
    {
        logger.LogInformation("Start request to get aasx file");

        var request = new SerializeAasxRequest(aasIds, submodelIds, includeConceptDescriptions);

        var response = await serializationHandler.GetAasxFileAsync(request, cancellationToken).ConfigureAwait(false);

        return response;
    }
}
