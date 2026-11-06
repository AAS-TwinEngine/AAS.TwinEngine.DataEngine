using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using Microsoft.Net.Http.Headers;

namespace AAS.TwinEngine.DataEngine.Api.Shared;

/// <summary>
/// Resolves the serialization format requested through HTTP content negotiation
/// as defined by the IDTA Serialization API.
/// </summary>
public static class SerializationFormatResolver
{
    private const double DefaultQuality = 1.0;

    /// <summary>
    /// Maps the <c>Accept</c> header to the serialization format with the highest quality factor.
    /// Falls back to <see cref="SerializationMediaTypes.DefaultFormat"/> when the client does not
    /// express a preference.
    /// </summary>
    /// <param name="acceptHeader">Raw <c>Accept</c> header value.</param>
    /// <exception cref="InvalidRequestHeaderException">None of the requested media types is supported.</exception>
    public static SerializationFormat Resolve(string? acceptHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptHeader))
        {
            return SerializationMediaTypes.DefaultFormat;
        }

        if (!MediaTypeHeaderValue.TryParseList([acceptHeader], out var mediaTypes) || mediaTypes is not { Count: > 0 })
        {
            throw UnsupportedMediaTypeException();
        }

        var format = SerializationMediaTypes.Supported
                     .Select((supportedMediaType, supportedIndex) =>
                     {
                         var match = mediaTypes
                                     .Select((mediaType, headerIndex) => new
                                     {
                                         Quality = mediaType.Quality ?? DefaultQuality,
                                         Specificity = GetSpecificity(mediaType, supportedMediaType),
                                         HeaderIndex = headerIndex
                                     })
                                     .Where(candidate => candidate.Specificity >= 0)
                                     .OrderByDescending(candidate => candidate.Specificity)
                                     .ThenByDescending(candidate => candidate.Quality)
                                     .ThenBy(candidate => candidate.HeaderIndex)
                                     .FirstOrDefault();

                         return new
                         {
                             Format = SerializationMediaTypes.FromMediaType(supportedMediaType),
                             Match = match,
                             SupportedIndex = supportedIndex
                         };
                     })
                     .Where(candidate => candidate.Match is { Quality: > 0 })
                     .OrderByDescending(candidate => candidate.Match!.Quality)
                     .ThenBy(candidate => candidate.Match!.HeaderIndex)
                     .ThenBy(candidate => candidate.SupportedIndex)
                     .Select(candidate => candidate.Format)
                     .FirstOrDefault();

        return format ?? throw UnsupportedMediaTypeException();
    }

    private static int GetSpecificity(MediaTypeHeaderValue mediaType, string supportedMediaType)
    {
        if (mediaType.MatchesAllTypes)
        {
            return 0;
        }

        if (mediaType.MatchesAllSubTypes)
        {
            return mediaType.Type.Equals("application", StringComparison.OrdinalIgnoreCase) ? 1 : -1;
        }

        return string.Equals(mediaType.MediaType.Value, supportedMediaType, StringComparison.OrdinalIgnoreCase) ? 2 : -1;
    }

    private static InvalidRequestHeaderException UnsupportedMediaTypeException()
        => new($"Unsupported Accept header. Supported media types are: {string.Join(", ", SerializationMediaTypes.Supported)}.");
}
