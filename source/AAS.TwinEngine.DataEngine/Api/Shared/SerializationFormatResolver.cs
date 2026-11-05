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

        var format = mediaTypes
                     .Where(mediaType => (mediaType.Quality ?? DefaultQuality) > 0)
                     .OrderByDescending(mediaType => mediaType.Quality ?? DefaultQuality)
                     .Select(TryMap)
                     .FirstOrDefault(mapped => mapped is not null);

        return format ?? throw UnsupportedMediaTypeException();
    }

    private static SerializationFormat? TryMap(MediaTypeHeaderValue mediaType)
    {
        if (mediaType.MatchesAllTypes)
        {
            return SerializationMediaTypes.DefaultFormat;
        }

        // "application/*" is satisfied by every supported media type, so the default is served.
        if (mediaType.MatchesAllSubTypes)
        {
            return mediaType.Type.Equals("application", StringComparison.OrdinalIgnoreCase)
                       ? SerializationMediaTypes.DefaultFormat
                       : null;
        }

        return SerializationMediaTypes.FromMediaType(mediaType.MediaType.Value);
    }

    private static InvalidRequestHeaderException UnsupportedMediaTypeException()
        => new($"Unsupported Accept header. Supported media types are: {string.Join(", ", SerializationMediaTypes.Supported)}.");
}
