namespace AAS.TwinEngine.DataEngine.Infrastructure.Logging;

/// <summary>
/// Provides log input sanitization to prevent log poisoning attacks.
/// Strips control characters and truncates values to prevent log injection,
/// ANSI escape sequence attacks, and log forging via newline injection.
/// </summary>
public static class LogSanitizerExtension
{
    private const int DefaultMaxLength = 500;

    /// <summary>
    /// Sanitizes a string value for safe inclusion in log output.
    /// Replaces control characters with their escaped representations and truncates to a maximum length.
    /// </summary>
    /// <param name="input">The potentially unsafe input string.</param>
    /// <param name="maxLength">Maximum allowed length before truncation. Defaults to 500.</param>
    /// <returns>A sanitized string safe for logging.</returns>
    public static string Sanitize(string? input, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var safeLength = Math.Min(input.Length, maxLength);
        var capacity = safeLength > int.MaxValue / 2 ? int.MaxValue : safeLength * 2;
        var sb = new System.Text.StringBuilder(capacity);

        foreach (var c in input)
        {
            if (sb.Length >= maxLength)
            {
                _ = sb.Append("...[truncated]");
                break;
            }

            switch (c)
            {
                case '\r':
                    _ = sb.Append("\\r");
                    break;
                case '\n':
                    _ = sb.Append("\\n");
                    break;
                case '\t':
                    _ = sb.Append("\\t");
                    break;
                case '\0':
                    _ = sb.Append("\\0");
                    break;
                case '\x1B':
                    _ = sb.Append("\\x1B");
                    break;
                case '\b':
                    _ = sb.Append("\\b");
                    break;
                case '\f':
                    _ = sb.Append("\\f");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        _ = sb.Append($"\\x{(int)c:X2}");
                    }
                    else
                    {
                        _ = sb.Append(c);
                    }
                    break;
            }
        }

        return sb.ToString();
    }
}
