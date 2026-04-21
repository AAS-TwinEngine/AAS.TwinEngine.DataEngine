using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;

public class TemplateMappingRulesValidator : IValidateOptions<TemplateManagementConfig>
{
    public ValidateOptionsResult Validate(string? name, TemplateManagementConfig options)
    {
        if (options == null)
        {
            throw new InvalidDependencyException(nameof(options));
        }

        var rules = options.TemplateMappingRules.AasIdExtractionRules;

        var basicValidation = ValidateRulesExist(rules);
        if (basicValidation != null)
        {
            return basicValidation;
        }

        var requireValidationPattern = rules!.Count > 1;

        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            var label = GetLabel(rule, i);

            var result =
                ValidatePattern(rule, label) ??
                ValidateIndex(rule, label) ??
                ValidateRegex(rule, label) ??
                ValidateSplit(rule, label) ??
                ValidateValidationPattern(rule, label, requireValidationPattern);

            if (result != null)
            {
                return result;
            }
        }

        return ValidateOptionsResult.Success;
    }

    private static ValidateOptionsResult? ValidateRulesExist(IList<AasIdExtractionRule>? rules)
    {
        if (rules == null || rules.Count == 0)
        {
            return ValidateOptionsResult.Fail("At least one AasIdExtractionRule is required.");
        }

        return null;
    }

    private static string GetLabel(AasIdExtractionRule rule, int index) =>
        !string.IsNullOrEmpty(rule.Description) ? rule.Description : $"Rule[{index}]";

    private static ValidateOptionsResult? ValidatePattern(AasIdExtractionRule rule, string label)
    {
        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            return ValidateOptionsResult.Fail($"AasIdExtractionRules: {label} has an empty Pattern.");
        }

        return null;
    }

    private static ValidateOptionsResult? ValidateIndex(AasIdExtractionRule rule, string label)
    {
        if (rule.Index < 1)
        {
            return ValidateOptionsResult.Fail($"AasIdExtractionRules: {label} Index must be >= 1.");
        }

        return null;
    }

    private static ValidateOptionsResult? ValidateRegex(AasIdExtractionRule rule, string label)
    {
        if (rule.Strategy != ExtractionStrategy.Regex)
        {
            return null;
        }

        if (!TryCompileRegex(rule.Pattern, out var error))
        {
            return ValidateOptionsResult.Fail($"AasIdExtractionRules: {label} has an invalid regex Pattern: {error}");
        }

        return null;
    }

    private static ValidateOptionsResult? ValidateSplit(AasIdExtractionRule rule, string label)
    {
        if (rule.Strategy == ExtractionStrategy.Split &&
            rule.EndIndex.HasValue &&
            rule.EndIndex.Value < rule.Index)
        {
            return ValidateOptionsResult.Fail(
                $"AasIdExtractionRules: {label} EndIndex ({rule.EndIndex}) must be >= Index ({rule.Index}).");
        }

        return null;
    }

    private static ValidateOptionsResult? ValidateValidationPattern(
        AasIdExtractionRule rule,
        string label,
        bool requireValidationPattern)
    {
        if (requireValidationPattern && string.IsNullOrWhiteSpace(rule.ValidationPattern))
        {
            return ValidateOptionsResult.Fail(
                $"AasIdExtractionRules: {label} is missing ValidationPattern. " +
                "ValidationPattern is required when multiple extraction rules are configured.");
        }

        if (!string.IsNullOrWhiteSpace(rule.ValidationPattern) &&
            !TryCompileRegex(rule.ValidationPattern, out var error))
        {
            return ValidateOptionsResult.Fail(
                $"AasIdExtractionRules: {label} has an invalid ValidationPattern: {error}");
        }

        return null;
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
