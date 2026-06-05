using System.Text.RegularExpressions;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Shared;

public static partial class InputValidationPatterns
{

    /// <summary>
    /// Validates that a string contains only XML-compatible characters (per AAS IDTA specification):
    /// source- https://aas-core-works.github.io/aas-core-meta/v3/LabelType.html
    /// </summary>
    [GeneratedRegex(
        @"^[\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD\uD800-\uDBFF\uDC00-\uDFFF]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    public static partial Regex XmlCharacterPattern();
}
