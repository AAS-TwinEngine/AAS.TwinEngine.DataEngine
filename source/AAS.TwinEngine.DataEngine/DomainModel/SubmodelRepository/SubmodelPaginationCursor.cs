using System.Text;

using Microsoft.AspNetCore.WebUtilities;

namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

/// <summary>
/// Two-field composite cursor for submodel pagination.
/// Wire format: Base64Url("{SubmodelId}|{AasId}")
/// </summary>
public sealed record SubmodelPaginationCursor(string? SubmodelId, string? AasId)
{
    private const char Separator = '|';

    public static SubmodelPaginationCursor? Decode(string? encodedCursor)
    {
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            return null;
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedCursor));
        }
        catch (FormatException)
        {
            return null;
        }

        var separatorIndex = decoded.IndexOf(Separator);

        if (separatorIndex < 0)
        {
            return null;
        }

        var submodelId = decoded[..separatorIndex];
        var aasId = decoded[(separatorIndex + 1)..];

        return new SubmodelPaginationCursor(
            string.IsNullOrEmpty(submodelId) ? null : submodelId,
            string.IsNullOrEmpty(aasId) ? null : aasId);
    }

    public static string? Encode(string? submodelId, string? aasId)
    {
        var logical = $"{submodelId ?? string.Empty}{Separator}{aasId ?? string.Empty}";
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(logical));
    }
}
