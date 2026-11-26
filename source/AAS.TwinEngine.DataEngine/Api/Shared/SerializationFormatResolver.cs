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
    // Candidate order breaks the remaining ties, so the default format wins for plain wildcards.
    private static readonly SerializationFormat[] Candidates = [SerializationMediaTypes.DefaultFormat, SerializationFormat.Json, SerializationFormat.Xml];

    /// <summary>
    /// Negotiates the serialization format from the <c>Accept</c> header (RFC 9110, section 12.5.1).
    /// Each supported format takes the quality of its most specific matching media range; formats with
    /// quality <c>0</c> are unacceptable. The highest quality wins, then the more specific range, then the
    /// earlier range in header order. Falls back to <see cref="SerializationMediaTypes.DefaultFormat"/> when
    /// the client does not express a preference.
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

        var best = Candidates
                   .Select(format => (Format: format, Match: FindMostSpecificMatch(format, mediaTypes)))
                   .Where(candidate => candidate.Match is { Quality: > 0 })
                   .OrderByDescending(candidate => candidate.Match!.Value.Quality)
                   .ThenByDescending(candidate => candidate.Match!.Value.Specificity)
                   .ThenBy(candidate => candidate.Match!.Value.Position)
                   .Select(candidate => (SerializationFormat?)candidate.Format)
                   .FirstOrDefault();

        return best ?? throw UnsupportedMediaTypeException();
    }

    private static (double Quality, int Specificity, int Position)? FindMostSpecificMatch(SerializationFormat format, IList<MediaTypeHeaderValue> mediaTypes)
    {
        (double Quality, int Specificity, int Position)? match = null;

        for (var position = 0; position < mediaTypes.Count; position++)
        {
            var specificity = GetSpecificity(format, mediaTypes[position]);
            if (specificity > (match?.Specificity ?? -1))
            {
                match = (mediaTypes[position].Quality ?? 1.0, specificity, position);
            }
        }

        return match;
    }

    /// <returns>2 for an exact media type, 1 for <c>application/*</c>, 0 for <c>*/*</c>, -1 when the range does not match.</returns>
    private static int GetSpecificity(SerializationFormat format, MediaTypeHeaderValue mediaType)
    {
        if (mediaType.MatchesAllTypes)
        {
            return 0;
        }

        if (mediaType.MatchesAllSubTypes)
        {
            return mediaType.Type.Equals("application", StringComparison.OrdinalIgnoreCase) ? 1 : -1;
        }

        return SerializationMediaTypes.FromMediaType(mediaType.MediaType.Value) == format ? 2 : -1;
    }

    private static InvalidRequestHeaderException UnsupportedMediaTypeException()
        => new($"Unsupported Accept header. Supported media types are: {string.Join(", ", SerializationMediaTypes.Supported)}.");
}
