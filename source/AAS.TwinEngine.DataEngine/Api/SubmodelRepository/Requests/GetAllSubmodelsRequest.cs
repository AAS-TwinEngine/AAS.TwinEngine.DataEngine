namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

public class GetAllSubmodelsRequest
{
    /// <summary>
    /// The value of the semantic id reference (UTF8-BASE64-URL-encoded).
    /// </summary>
    public string? SemanticId { get; set; }

    /// <summary>
    /// The Asset Administration Shell's IdShort.
    /// </summary>
    public string? IdShort { get; set; }

    /// <summary>
    /// The maximum number of elements in the response array.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// A server-generated identifier retrieved from pagingMetadata.
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Determines the structural depth of the resource content.
    /// </summary>
    public Level? Level { get; set; }

    /// <summary>
    /// Determines to which extent the resource is serialized.
    /// </summary>
    public Extent? Extent { get; set; }
}

public enum Level
{
    deep,
    core
}

public enum Extent
{
    withBlobValue,
    withoutBlobValue
}
