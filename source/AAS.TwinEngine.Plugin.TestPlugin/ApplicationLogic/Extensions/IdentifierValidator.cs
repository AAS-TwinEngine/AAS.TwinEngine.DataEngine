using System.Text.RegularExpressions;

namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Extensions;

public static class IdentifierValidator
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex XssPattern = new(@"<[^>]*on\w+\s*=|<\s*script|<\s*/\s*script", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex SqlInjectionPattern = new(@"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b)|('|(--)|;|\/\*|\*\/|xp_)", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex PathTraversalPattern = new(@"(\.\.[/\\])|(%2e%2e[/\\])|(\.\.[%2f%5c])", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);

    private static readonly string[] DangerousProtocols =
    [
        "JAVASCRIPT:",
        "DATA:",
        "VBSCRIPT:",
        "FILE:",
        "ABOUT:"
    ];

    private static readonly string[] DangerousPatterns =
    [
        "<SCRIPT",
        "</SCRIPT",
        "ONERROR=",
        "ONLOAD=",
        "ONCLICK=",
        "EVAL(",
        "EXPRESSION(",
        "JAVASCRIPT:",
        "VBSCRIPT:",
        "\0",
        "%00"
    ];

    public static bool IsValidIdentifier(this string identifier, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        if (ContainsDangerousProtocol(identifier))
        {
            LogIdentifierWarning(logger, "Identifier contains dangerous protocol", identifier.Length);
            return false;
        }

        if (ContainsDangerousPattern(identifier))
        {
            LogIdentifierWarning(logger, "Identifier contains dangerous pattern", identifier.Length);
            return false;
        }

        if (ContainsXssPattern(identifier))
        {
            LogIdentifierWarning(logger, "Identifier contains potential XSS pattern", identifier.Length);
            return false;
        }

        if (ContainsSqlInjectionPattern(identifier))
        {
            LogIdentifierWarning(logger, "Identifier contains potential SQL injection pattern", identifier.Length);
            return false;
        }

        if (!ContainsPathTraversalPattern(identifier))
        {
            return true;
        }

        LogIdentifierWarning(logger, "Identifier contains potential path traversal pattern", identifier.Length);
        return false;
    }

    private static void LogIdentifierWarning(ILogger? logger, string message, int identifierLength)
    {
        logger?.LogWarning("{Message}. Identifier length: {Length}", message, identifierLength);
    }

    private static bool ContainsDangerousProtocol(string identifier) => DangerousProtocols.Any(protocol => identifier.Contains(protocol, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsDangerousPattern(string identifier) => DangerousPatterns.Any(pattern => identifier.Contains(pattern, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsXssPattern(string identifier)
    {
        try
        {
            return XssPattern.IsMatch(identifier);
        }
        catch (RegexMatchTimeoutException)
        {
            return true;
        }
    }

    private static bool ContainsSqlInjectionPattern(string identifier)
    {
        try
        {
            return SqlInjectionPattern.IsMatch(identifier);
        }
        catch (RegexMatchTimeoutException)
        {
            return true;
        }
    }

    private static bool ContainsPathTraversalPattern(string identifier)
    {
        try
        {
            return PathTraversalPattern.IsMatch(identifier);
        }
        catch (RegexMatchTimeoutException)
        {
            return true;
        }
    }
}

