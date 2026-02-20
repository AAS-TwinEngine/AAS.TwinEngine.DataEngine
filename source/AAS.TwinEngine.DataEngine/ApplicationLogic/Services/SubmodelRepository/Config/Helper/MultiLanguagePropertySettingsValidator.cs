using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config.Helper;

public partial class MultiLanguagePropertySettingsValidator : IValidateOptions<MultiLanguagePropertySettings>
{
    public ValidateOptionsResult Validate(string? name, MultiLanguagePropertySettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.DefaultLanguages == null || options.DefaultLanguages.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        var invalidLanguages = new List<string>();

        foreach (var language in options.DefaultLanguages)
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
                                              $"Invalid BCP-47 language tag(s) in {MultiLanguagePropertySettings.Section}.DefaultLanguages: " +
                                              $"{string.Join(", ", invalidLanguages)}. Note: Use hyphens (-) not underscores (_).");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidBcp47LanguageTag(string languageTag) => Bcp47Pattern().IsMatch(languageTag);

    /// <summary>
    /// BCP-47 language tag pattern.
    /// Matches: "en", "en-US", "de", "fr-FR", "zh-Hans", "zh-Hans-CN"
    /// Rejects: "en_US", "en-", "123"
    /// </summary>
    [GeneratedRegex(@"^[a-z]{2,4}(-[A-Z][a-z]{3})?(-([A-Z]{2}|[0-9]{3}))?$", RegexOptions.Compiled)]
    private static partial Regex Bcp47Pattern();
}
