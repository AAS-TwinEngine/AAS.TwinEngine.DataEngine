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
    /// <summary>
    /// Maps the <c>Accept</c> header to the first supported serialization format in header order.
    /// Quality factors (<c>q</c>) are not evaluated, as the IDTA Serialization API does not define them.
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

        foreach (var mediaType in mediaTypes)
        {
            var format = ToFormat(mediaType);

            if (format is not null)
            {
                return format.Value;
            }
        }

        throw UnsupportedMediaTypeException();
    }

    private static SerializationFormat? ToFormat(MediaTypeHeaderValue mediaType)
    {
        if (mediaType.MatchesAllTypes)
        {
            return SerializationMediaTypes.DefaultFormat;
        }

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
