using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

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
            throw new InvalidDependencyException(nameof(options), logger);
        }

        var rules = options.TemplateMappingRules.AasIdExtractionRules;

        var basicValidation = ValidateRulesExist(rules);
        if (basicValidation != null)
        {
            _logger.LogError("Validation failed: No extraction rules found");
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
                _logger.LogError("Validation failed for {Label}", label);
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

    private static string GetLabel(int index) => $"Rule[{index}]";

    private ValidateOptionsResult? ValidatePattern(AasIdExtractionRule rule, string label)
    {
        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            _logger.LogError("AasIdExtractionRules: {label} has an empty Pattern.", label);
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        return null;
    }

    private ValidateOptionsResult? ValidateIndex(AasIdExtractionRule rule, string label)
    {
        if (rule.Index < 0)
        {
            _logger.LogError("AasIdExtractionRules: {label} Index must be >= 0.", label);
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
            _logger.LogError("AasIdExtractionRules: {label} has an invalid regex Pattern: {errorMsg}", label, errorMsg);
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
            _logger.LogError("AasIdExtractionRules: {label} EndIndex ({rule.EndIndex}) must be >= Index ({rule.Index}).", label);
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
            _logger.LogError("AasIdExtractionRules: {label} is missing ValidationPattern. " +
                    "ValidationPattern is required for Regex rules when multiple extraction rules are configured.", label);
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        if (!string.IsNullOrWhiteSpace(rule.ValidationPattern) &&
            !TryCompileRegex(rule.ValidationPattern, out var errorMsg))
        {
            _logger.LogError("AasIdExtractionRules: {label} has an invalid ValidationPattern: {errorMsg}", errorMsg);
            return ValidateOptionsResult.Fail(GenericValidationError);
        }

        return null;
    }

    private bool TryCompileRegex(string pattern, out string? error)
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
            _logger.LogError(ex, "Regex compilation failed for pattern: {Pattern}", pattern);
            return false;
        }
    }
}
