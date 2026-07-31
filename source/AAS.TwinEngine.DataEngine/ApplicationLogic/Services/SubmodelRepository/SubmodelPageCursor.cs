using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

/// <summary>
/// Opaque cursor for the "Get All Submodels" flow. A single product (AAS) can hold multiple
/// submodels and may be only partially delivered within one page, so resuming requires more
/// state than a single submodel id.
/// </summary>
/// <param name="PluginPageCursor">
/// The plugin cursor that fetched the batch the client is currently positioned in
/// (<c>null</c> for the first plugin page). On resume the same batch is re-fetched so products
/// that were returned alongside a partially consumed product are not lost.
/// </param>
/// <param name="CurrentAasId">The AAS id of the partially consumed product to resume within.</param>
/// <param name="LastSubmodelId">The last submodel id already delivered inside <paramref name="CurrentAasId"/>.</param>
public sealed record SubmodelPageCursor(string? PluginPageCursor, string CurrentAasId, string LastSubmodelId)
{
    // Logical layout: {PluginPageCursor}|{CurrentAasId}|{LastSubmodelId}
    private const char Separator = '|';
    private const int FieldCount = 3;

    /// <summary>Encodes the cursor into the opaque Base64Url token returned to the client.</summary>
    public string Encode() =>
        string.Join(Separator, PluginPageCursor, CurrentAasId, LastSubmodelId).EncodeBase64Url();

    /// <summary>
    /// Decodes a client supplied cursor. Returns <c>null</c> when the cursor is absent or does not
    /// have the expected shape, which callers treat as "start from the first page".
    /// </summary>
    public static SubmodelPageCursor? TryDecode(string? encodedCursor)
    {
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            return null;
        }

        var parts = encodedCursor.DecodeBase64Url().Split(Separator);
        if (parts.Length != FieldCount)
        {
            return null;
        }

        var pluginPageCursor = string.IsNullOrEmpty(parts[0]) ? null : parts[0];
        return new SubmodelPageCursor(pluginPageCursor, parts[1], parts[2]);
    }
}
