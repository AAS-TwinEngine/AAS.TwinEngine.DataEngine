using System.Text.Json.Serialization;
using System.ComponentModel;

namespace AAS.TwinEngine.DataEngine.Api.Shared;

/// <summary>
/// Cursor-based pagination metadata.
/// </summary>
public class PagingMetaDataDto
{
    /// <summary>
    /// Opaque cursor token that can be used to request the next page.
    /// </summary>
    /// <example>eyJvZmZzZXQiOjEwMH0</example>
    [JsonPropertyName("cursor")]
    [Description("Opaque cursor token for pagination continuation.")]
    public string? Cursor { get; set; }
}
