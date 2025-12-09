using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

using Microsoft.AspNetCore.WebUtilities;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

public static class Base64UrlExtensions
{
    /// <summary>
    /// Decodes a Base64 URL encoded string to its original UTF-8 representation.
    /// </summary>
    /// <param name="encoded">The Base64 URL encoded string.</param>
    /// <param name="logger"></param>
    /// <returns>The decoded UTF-8 string, or empty if input is null or whitespace.</returns>
    /// <exception cref="InvalidUserInputException">Thrown when the string cannot be decoded.</exception>
    public static string DecodeBase64Url(this string encoded, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            logger?.LogError("Identifier cannot be null or empty.");
            throw new InvalidUserInputException();
        }

        try
        {
            var bytes = WebEncoders.Base64UrlDecode(encoded);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to decode Base64 URL string: {Encoded}", encoded);
            throw new InvalidUserInputException();
        }
    }

    /// <summary>
    /// Encodes a UTF-8 string to Base64 URL format.
    /// </summary>
    /// <param name="plainText">The plain UTF-8 string to encode.</param>
    /// <param name="logger"></param>
    /// <returns>The Base64 URL encoded string, or empty if input is null or whitespace.</returns>
    /// <exception cref="InternalDataProcessingException">Thrown when the string cannot be encoded.</exception>
    public static string EncodeBase64Url(this string plainText, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            return WebEncoders.Base64UrlEncode(bytes);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to encode string to Base64 URL format: {PlainText}", plainText);
            throw new InvalidUserInputException();
        }
    }
}
