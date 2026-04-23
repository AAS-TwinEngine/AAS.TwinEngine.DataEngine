using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config.Helpers;

public class TemplateMappingRulesValidator(ILogger<TemplateMappingRulesValidator> logger) : IValidateOptions<TemplateManagementConfig>
{
    private readonly ILogger<TemplateMappingRulesValidator> _logger = logger;
    private const string GenericValidationError = "Invalid AasIdExtractionRules configuration.";

    public ValidateOptionsResult Validate(string? name, TemplateManagementConfig options)
    {
        if (options == null)
        {
            _logger.LogError("TemplateManagementConfig options are null");
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        var rules = options.TemplateMappingRules.AasIdExtractionRules;

        var basicValidation = ValidateRulesExist(rules);
        if (basicValidation != null)
        {
            _logger.LogError("Invalid AasIdExtractionRules configuration.");
            return basicValidation;
        }

        var requireValidationPattern = rules!.Count > 1;

        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            var label = GetLabel(i);

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

    private ValidateOptionsResult? ValidateRulesExist(IList<AasIdExtractionRule>? rules)
    {
        if (rules == null || rules.Count == 0)
        {
            _logger.LogError("At least one AasIdExtractionRule is required.");
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        return null;
    }

    private static string GetLabel(int index) => $"AasIdExtractionRule[{index}]";

    private ValidateOptionsResult? ValidatePattern(AasIdExtractionRule rule, string label)
    {
        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            _logger.LogError("AasIdExtractionRules: {Label} has an empty Pattern.", label);
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        return null;
    }

    private ValidateOptionsResult? ValidateIndex(AasIdExtractionRule rule, string label)
    {
        if (rule.Index < 0)
        {
            _logger.LogError("AasIdExtractionRules: {Label} Index must be >= 0.", label);
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        return null;
    }

    private ValidateOptionsResult? ValidateRegex(AasIdExtractionRule rule, string label)
    {
        if (rule.Strategy != ExtractionStrategy.Regex)
        {
            return null;
        }

        if (!TryCompileRegex(rule.Pattern, out var errorMsg))
        {
            _logger.LogError("AasIdExtractionRules: {Label} has an invalid regex Pattern: {ErrorMsg}", label, errorMsg);
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        return null;
    }

    private ValidateOptionsResult? ValidateSplit(AasIdExtractionRule rule, string label)
    {
        if (rule.Strategy == ExtractionStrategy.Split &&
            rule.EndIndex.HasValue &&
            rule.EndIndex.Value < rule.Index)
        {
            _logger.LogError("AasIdExtractionRules: {Label} EndIndex ({EndIndex}) must be >= Index ({Index}).", label, rule.EndIndex, rule.Index);
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        return null;
    }

    private ValidateOptionsResult? ValidateValidationPattern(
        AasIdExtractionRule rule,
        string label,
        bool requireValidationPattern)
    {
        if (rule.Strategy == ExtractionStrategy.Regex &&
                requireValidationPattern &&
                string.IsNullOrWhiteSpace(rule.ValidationPattern))
        {
            _logger.LogError("AasIdExtractionRules: {Label} is missing ValidationPattern. " +
                    "ValidationPattern is required for Regex rules when multiple extraction rules are configured.", label);
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        if (!string.IsNullOrWhiteSpace(rule.ValidationPattern) &&
            !TryCompileRegex(rule.ValidationPattern, out var errorMsg))
        {
            _logger.LogError("AasIdExtractionRules: {Label} has an invalid ValidationPattern: {ErrorMsg}", label, errorMsg);
            return ValidateOptionsResult.Fail(GenericValidationError);
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
