using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

/// <summary>
/// Validates identifiers to prevent injection attacks through ID-based URLs.
/// </summary>
public static partial class IdentifierValidator
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex XssPattern = new(@"<[^>]*on\w+\s*=|<\s*script|<\s*/\s*script", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex SqlInjectionPattern = new(@"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b)|('|(--)|;|\/\*|\*\/|xp_)", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex PathTraversalPattern = new(@"(\.\.[/\\])|(%2e%2e[/\\])|(\.\.[%2f%5c])", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);

    // IdShort validation: allows alphanumeric, dot, underscore, hyphen, and square brackets (for array indexing)
    // Pattern: ^[a-zA-Z0-9._\-\[\]%]+$
    private static readonly Regex IdShortPattern = IdShortRegex();

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

    /// <summary>
    /// Validates that an identifier does not contain malicious URL patterns or injection attempts.
    /// </summary>
    /// <param name="identifier">The identifier to validate</param>
    /// <param name="parameterName">The name of the parameter being validated (for exception messages)</param>
    /// <param name="logger">Optional logger for validation failures</param>
    /// <exception cref="InvalidUserInputException">Thrown when the identifier contains malicious patterns</exception>
    public static void ValidateIdentifier(this string identifier, string parameterName, ILogger? logger = null)
    {
        if (identifier.IsValidIdentifier(logger))
        {
            return;
        }

        logger?.LogError("Identifier validation failed for parameter: {ParameterName}. Identifier may contain malicious patterns.", parameterName);
        throw new InvalidUserInputException();
    }

    /// <summary>
    /// Validates that an idShortPath does not contain malicious patterns.
    /// IdShortPath format: element.submodelElement[0].property (allows dots, alphanumeric, brackets, underscore, hyphen)
    /// </summary>
    /// <param name="idShortPath">The idShortPath to validate</param>
    /// <param name="parameterName">The name of the parameter being validated (for exception messages)</param>
    /// <param name="logger">Optional logger for validation failures</param>
    /// <exception cref="InvalidUserInputException">Thrown when the idShortPath contains malicious patterns</exception>
    public static void ValidateIdShortPath(this string idShortPath, string parameterName, ILogger? logger = null)
    {
        if (idShortPath.IsValidIdShortPath(logger))
        {
            return;
        }

        logger?.LogError("IdShortPath validation failed for parameter: {ParameterName}. IdShortPath may contain malicious patterns.", parameterName);
        throw new InvalidUserInputException();
    }

    /// <summary>
    /// Checks if an identifier contains potentially malicious URL patterns.
    /// </summary>
    /// <param name="identifier">The identifier to check</param>
    /// <param name="logger">Optional logger for validation warnings</param>
    /// <returns>True if the identifier appears safe, false otherwise</returns>
    public static bool IsValidIdentifier(this string identifier, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        if (ContainsDangerousProtocol(identifier))
        {
            logger?.LogWarning("Identifier contains dangerous protocol: {Identifier}", identifier);
            return false;
        }

        if (ContainsDangerousPattern(identifier))
        {
            logger?.LogWarning("Identifier contains dangerous pattern: {Identifier}", identifier);
            return false;
        }

        if (ContainsXssPattern(identifier))
        {
            logger?.LogWarning("Identifier contains potential XSS pattern: {Identifier}", identifier);
            return false;
        }

        if (ContainsSqlInjectionPattern(identifier))
        {
            logger?.LogWarning("Identifier contains potential SQL injection pattern: {Identifier}", identifier);
            return false;
        }

        if (!ContainsPathTraversalPattern(identifier))
        {
            return true;
        }

        logger?.LogWarning("Identifier contains potential path traversal pattern: {Identifier}", identifier);
        return false;
    }

    /// <summary>
    /// Checks if an idShortPath contains potentially malicious patterns.
    /// IdShortPath format: element.subElement[0].property
    /// Allows: alphanumeric, dot (.), underscore (_), hyphen (-), square brackets ([]), and URL-encoded brackets (%5B, %5D)
    /// </summary>
    /// <param name="idShortPath">The idShortPath to check</param>
    /// <param name="logger">Optional logger for validation warnings</param>
    /// <returns>True if the idShortPath appears safe, false otherwise</returns>
    public static bool IsValidIdShortPath(this string idShortPath, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(idShortPath))
        {
            logger?.LogWarning("IdShortPath is null or whitespace");
            return false;
        }

        if (ContainsDangerousProtocol(idShortPath))
        {
            logger?.LogWarning("IdShortPath contains dangerous protocol: {IdShortPath}", idShortPath);
            return false;
        }

        if (ContainsDangerousPattern(idShortPath))
        {
            logger?.LogWarning("IdShortPath contains dangerous pattern: {IdShortPath}", idShortPath);
            return false;
        }

        if (ContainsXssPattern(idShortPath))
        {
            logger?.LogWarning("IdShortPath contains potential XSS pattern: {IdShortPath}", idShortPath);
            return false;
        }

        if (ContainsSqlInjectionPattern(idShortPath))
        {
            logger?.LogWarning("IdShortPath contains potential SQL injection pattern: {IdShortPath}", idShortPath);
            return false;
        }

        if (ContainsPathTraversalPattern(idShortPath))
        {
            logger?.LogWarning("IdShortPath contains potential path traversal pattern: {IdShortPath}", idShortPath);
            return false;
        }

        try
        {
            if (!IdShortPattern.IsMatch(idShortPath))
            {
                logger?.LogWarning("IdShortPath contains invalid characters: {IdShortPath}", idShortPath);
                return false;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            logger?.LogWarning("IdShortPath validation timeout: {IdShortPath}", idShortPath);
            return false;
        }

        return true;
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

    /// <summary>
    /// Matches valid idShortPath format:
    /// - Allows: alphanumeric (a-z, A-Z, 0-9)
    /// - Allows: dot (.), underscore (_), hyphen (-)
    /// - Allows: square brackets ([ and ]) for array indexing
    /// - Allows: URL-encoded brackets (%5B, %5D)
    /// Pattern: ^[a-zA-Z0-9._\-\[\]%]+$
    /// </summary>
    [GeneratedRegex(@"^[a-zA-Z0-9._\-\[\]%]+$", RegexOptions.None, 100)]
    private static partial Regex IdShortRegex();
}
