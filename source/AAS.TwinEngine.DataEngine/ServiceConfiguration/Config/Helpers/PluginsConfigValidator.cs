using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config.Helpers;

/// <summary>
/// Validates the MultiLanguageProperty settings within PluginsConfig.
/// Equivalent to the old MultiLanguagePropertySettingsValidator but for V2 POCO.
/// </summary>
public partial class PluginsConfigValidator : IValidateOptions<PluginsConfig>
{
    public ValidateOptionsResult Validate(string? name, PluginsConfig options)
    {
        if (options is null)
        {
            throw new InvalidDependencyException(nameof(options));
        }

        var defaultLanguages = options.MultiLanguageProperty.DefaultLanguages;
        if (defaultLanguages == null || defaultLanguages.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        var invalidLanguages = new List<string>();
        foreach (var language in defaultLanguages)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                invalidLanguages.Add("(empty)");
                continue;
            }

            if (!IsValidBcp47LanguageTag(language))
            {
                invalidLanguages.Add(language);
            }
        }

        if (invalidLanguages.Count > 0)
        {
            return ValidateOptionsResult.Fail(
                $"Invalid BCP-47 or Null language tag(s) in {PluginsConfig.Section}.MultiLanguageProperty.DefaultLanguages: " +
                $"{string.Join(", ", invalidLanguages)}.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidBcp47LanguageTag(string languageTag) => Bcp47Pattern().IsMatch(languageTag);

    [GeneratedRegex(@"^[a-z]{2,4}(-[A-Z][a-z]{3})?(-([A-Z]{2}|[0-9]{3}))?$", RegexOptions.Compiled)]
    private static partial Regex Bcp47Pattern();
}
