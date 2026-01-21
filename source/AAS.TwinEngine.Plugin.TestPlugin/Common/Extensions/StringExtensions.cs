using System.Text;

using Microsoft.AspNetCore.WebUtilities;

namespace AAS.TwinEngine.Plugin.TestPlugin.Common.Extensions;

public static class StringExtensions
{
    public static string DecodeBase64(this string encodedValue)
    {
        if (string.IsNullOrEmpty(encodedValue))
            throw new ArgumentException("Input string cannot be null or empty.", nameof(encodedValue));

        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedValue));
        }
        catch (FormatException)
        {
            throw new ArgumentException("The provided string is not a valid Base64 encoded string.", nameof(encodedValue));
        }
    }

    public static string EncodeToBase64(this string plainValue)
    {
        if (string.IsNullOrEmpty(plainValue))
            throw new ArgumentException("Input string cannot be null or empty.", nameof(plainValue));

        var bytes = System.Text.Encoding.UTF8.GetBytes(plainValue);
        return Convert.ToBase64String(bytes);
    }
}
