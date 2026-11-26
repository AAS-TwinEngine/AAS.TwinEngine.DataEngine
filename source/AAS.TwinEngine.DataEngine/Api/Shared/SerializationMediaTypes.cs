using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.Api.Shared;

/// <summary>
/// Media types defined by the IDTA Serialization API and their mapping to <see cref="SerializationFormat"/>.
/// </summary>
public static class SerializationMediaTypes
{
    public const string AasxPackage = "application/asset-administration-shell-package+xml";

    public const string Json = "application/json";

    public const string Xml = "application/xml";

    /// <summary>
    /// Format returned when the client does not express a preference (<c>Accept</c> absent, <c>*/*</c> or <c>application/*</c>).
    /// </summary>
    public const SerializationFormat DefaultFormat = SerializationFormat.AasxPackage;

    public static IReadOnlyList<string> Supported { get; } = [AasxPackage, Json, Xml];

    public static SerializationFormat? FromMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return null;
        }

        return mediaType.Trim().ToLowerInvariant() switch
        {
            AasxPackage => SerializationFormat.AasxPackage,
            Json => SerializationFormat.Json,
            Xml => SerializationFormat.Xml,
            _ => null
        };
    }

    public static string ToMediaType(SerializationFormat format)
        => format switch
        {
            SerializationFormat.Json => Json,
            SerializationFormat.Xml => Xml,
            _ => AasxPackage
        };

    public static string ToFileExtension(SerializationFormat format)
        => format switch
        {
            SerializationFormat.Json => "json",
            SerializationFormat.Xml => "xml",
            _ => "aasx"
        };
}
