using System.Text;

namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;

/// <summary>
/// IDTA-compliant Base64URL encoding used for AAS identifiers in URL segments.
/// </summary>
internal static class Base64Url
{
    public static string Encode(string identifier)
    {
        var bytes = Encoding.UTF8.GetBytes(identifier);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
