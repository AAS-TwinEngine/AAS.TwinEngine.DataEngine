using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;

public class SerializationHandler(
    ILogger<SerializationHandler> logger,
    ISerializationService serializationService) : ISerializationHandler
{
    public Task<FileStreamResult> GetSerializationAsync(
        SerializationRequest request,
        CancellationToken cancellationToken)
        => GetSerializationResourceAsync(
                                         request.AasIdentifier,
                                         request.SubmodelIdentifier,
                                         request.IncludeConceptDescriptions,
                                         SerializationFormatResolver.Resolve(request.AcceptHeader),
                                         cancellationToken);

    private async Task<FileStreamResult> GetSerializationResourceAsync(
        IList<string>? encodedAasIds,
        IList<string>? encodedSubmodelIds,
        bool? includeConceptDescriptions,
        SerializationFormat format,
        CancellationToken cancellationToken)
    {
        var decodedAasIds = ValidateAndDecodeIdentifiers(encodedAasIds!, "AasIdentifier");
        var decodedSubmodelIds = ValidateAndDecodeIdentifiers(encodedSubmodelIds!, "SubmodelIdentifier");
        var conceptDescriptions = includeConceptDescriptions ?? false;

        logger.LogInformation("Start serializing AAS IDs: {AasIds} and Submodel IDs: {SubmodelIds} as {Format}",
                              decodedAasIds,
                              decodedSubmodelIds,
                              format);

        var stream = await serializationService
                           .GetSerializationStreamAsync(decodedAasIds, decodedSubmodelIds, conceptDescriptions, format, cancellationToken)
                           .ConfigureAwait(false);

        ValidateResourceExists(stream, "Serialization", decodedAasIds.First());

        var fileName = $"{decodedAasIds.First()}.{SerializationMediaTypes.ToFileExtension(format)}";
        return new FileStreamResult(stream, SerializationMediaTypes.ToMediaType(format))
        {
            FileDownloadName = fileName
        };
    }

    private IList<string> ValidateAndDecodeIdentifiers(IList<string> encodedIds, string resourceName)
    {
        if (encodedIds is null || encodedIds.Count == 0)
        {
            logger.LogError("{ResourceName} must not be null or empty.", resourceName);
            throw new InternalDataProcessingException();
        }

        return [.. encodedIds.Select(encodedId => encodedId.DecodeBase64Url(logger))];
    }

    private void ValidateResourceExists<T>(T? result, string resourceName, string key) where T : class
    {
        if (result == null)
        {
            logger.LogWarning("{ResourceName} not found for key: {Key}", resourceName, key);
            throw new ResourceNotFoundException();
        }
    }
}
