using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

using Microsoft.AspNetCore.WebUtilities;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

public static class Base64UrlExtensions
{
    private const int MaxIdentifierLength = 2048;

    /// <summary>
    /// Decodes a Base64 URL encoded string to its original UTF-8 representation and validates it for security.
    /// </summary>
    /// <param name="encoded">The Base64 URL encoded string.</param>
    /// <param name="logger">Optional logger for validation failures</param>
    /// <returns>The decoded and validated UTF-8 string.</returns>
    /// <exception cref="InvalidUserInputException">Thrown when the string cannot be decoded or fails validation.</exception>
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
            var decoded = Encoding.UTF8.GetString(bytes);

            if (decoded.Length > MaxIdentifierLength)
            {
                logger?.LogError("Decoded identifier exceeds maximum length of {MaxLength} characters: actual length {ActualLength}",
                    MaxIdentifierLength, decoded.Length);
                throw new InvalidUserInputException();
            }

            if (decoded.IsValidIdentifier(logger))
            {
                return decoded;
            }

            logger?.LogError("Decoded identifier contains malicious patterns.");
            throw new InvalidUserInputException();
        }
        catch (InvalidUserInputException)
        {
            throw;
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
    /// <param name="logger">Optional logger for encoding failures</param>
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
            var encoded = WebEncoders.Base64UrlEncode(bytes);

            if (encoded.Length <= MaxIdentifierLength)
            {
                return encoded;
            }

            logger?.LogError("Decoded identifier exceeds maximum length of {MaxLength} characters: actual length {ActualLength}",
                             MaxIdentifierLength, encoded.Length);
            throw new InternalDataProcessingException();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to encode string to Base64 URL format: {PlainText}", plainText);
            throw new InternalDataProcessingException();
        }
    }
}
