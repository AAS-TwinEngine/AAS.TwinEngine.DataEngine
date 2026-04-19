using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;

public class TemplateMappingRulesValidator : IValidateOptions<TemplateManagementConfig>
{
    public ValidateOptionsResult Validate(string? name, TemplateManagementConfig options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var rules = options.TemplateMappingRules.AasIdExtractionRules;

        if (rules == null || rules.Count == 0)
        {
            return ValidateOptionsResult.Fail("At least one AasIdExtractionRule is required.");
        }

        var requireValidationPattern = rules.Count > 1;

        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            var label = !string.IsNullOrEmpty(rule.Description) ? rule.Description : $"Rule[{i}]";

            if (string.IsNullOrWhiteSpace(rule.Pattern))
            {
                return ValidateOptionsResult.Fail($"AasIdExtractionRules: {label} has an empty Pattern.");
            }

            if (rule.Index < 1)
            {
                return ValidateOptionsResult.Fail($"AasIdExtractionRules: {label} Index must be >= 1.");
            }

            if (rule.Strategy == ExtractionStrategy.Regex)
            {
                if (!TryCompileRegex(rule.Pattern, out var error))
                {
                    return ValidateOptionsResult.Fail($"AasIdExtractionRules: {label} has an invalid regex Pattern: {error}");
                }
            }

            if (rule.Strategy == ExtractionStrategy.Split && rule.EndIndex.HasValue && rule.EndIndex.Value < rule.Index)
            {
                return ValidateOptionsResult.Fail($"AasIdExtractionRules: {label} EndIndex ({rule.EndIndex}) must be >= Index ({rule.Index}).");
            }

            if (requireValidationPattern && string.IsNullOrWhiteSpace(rule.ValidationPattern))
            {
                return ValidateOptionsResult.Fail(
                    $"AasIdExtractionRules: {label} is missing ValidationPattern. " +
                    "ValidationPattern is required when multiple extraction rules are configured.");
            }

            if (!string.IsNullOrWhiteSpace(rule.ValidationPattern) && !TryCompileRegex(rule.ValidationPattern, out var vpError))
            {
                return ValidateOptionsResult.Fail($"AasIdExtractionRules: {label} has an invalid ValidationPattern: {vpError}");
            }
        }

        return ValidateOptionsResult.Success;
    }

    private static bool TryCompileRegex(string pattern, out string? error)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
            error = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
