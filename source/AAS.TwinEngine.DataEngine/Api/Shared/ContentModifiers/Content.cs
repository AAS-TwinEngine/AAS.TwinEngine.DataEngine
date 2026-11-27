namespace AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;

/// <summary>
/// Serialization modifier "Content" (IDTA-01002), exposed via the path suffixes
/// <c>/$metadata</c>, <c>/$value</c>, <c>/$reference</c> and <c>/$path</c>.
/// </summary>
public enum Content
{
    Normal,
    Metadata,
    Value,
    Reference,
    Path
}
