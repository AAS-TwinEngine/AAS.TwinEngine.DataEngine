using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

public class ShellTemplateMappingProvider(ILogger<ShellTemplateMappingProvider> logger, IOptions<TemplateManagementConfig> options) : IShellTemplateMappingProvider
{
    private readonly ILogger<ShellTemplateMappingProvider> _logger = logger ?? throw new InvalidDependencyException(nameof(logger), logger);
    private readonly IList<ShellTemplateMappings> _shellTemplateMappings = options.Value.TemplateMappingRules.ShellTemplateMappings ?? throw new InvalidDependencyException(nameof(options.Value.TemplateMappingRules.ShellTemplateMappings), logger);
    private readonly IList<AasIdExtractionRule> _aasIdExtractionRules = options.Value.TemplateMappingRules.AasIdExtractionRules ?? throw new InvalidDependencyException(nameof(options.Value.TemplateMappingRules.AasIdExtractionRules), logger);
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public string? GetTemplateId(string aasIdentifier)
    {
        var productId = GetProductIdFromRule(aasIdentifier);

        var templateId = _shellTemplateMappings
            .FirstOrDefault(mapping => mapping.Pattern
                                              .Any(pattern => Regex.IsMatch(productId, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, _regexTimeout)))
            ?.TemplateId;

        if (templateId is not null)
        {
            return templateId;
        }

        _logger.LogError("No matching template found for shell: {AasIdentifier}", aasIdentifier);
        throw new ResourceNotFoundException();
    }

    public string GetProductIdFromRule(string aasIdentifier)
    {
        foreach (var rule in _aasIdExtractionRules)
        {
            var extracted = rule.Strategy switch
            {
                ExtractionStrategy.Regex => TryExtractWithRegex(aasIdentifier, rule),
                ExtractionStrategy.Split => TryExtractWithSplit(aasIdentifier, rule),
                _ => null
            };

            if (string.IsNullOrEmpty(extracted))
            {
                continue;
            }

            if (string.Equals(extracted, aasIdentifier, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(rule.ValidationPattern) &&
                !Regex.IsMatch(extracted, rule.ValidationPattern, RegexOptions.None, _regexTimeout))
            {
                continue;
            }

            _logger.LogInformation("Successfully extracted ProductId: {ProductId}", extracted);
            return extracted;
        }

        _logger.LogError("ProductId could not be extracted from the provided aas Identifier.");
        throw new ResourceNotFoundException();
    }

    private string? TryExtractWithRegex(string input, AasIdExtractionRule rule)
    {
        var match = Regex.Match(input, rule.Pattern, RegexOptions.None, _regexTimeout);

        if (!match.Success)
        {
            return null;
        }

        if (rule.Index >= match.Groups.Count)
        {
            return null;
        }

        var value = match.Groups[rule.Index].Value;

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? TryExtractWithSplit(string input, AasIdExtractionRule rule)
    {
        var parts = input.Split(rule.Pattern);

        var startIndex = rule.Index;
        var endIndex = rule.EndIndex ?? rule.Index;

        if (endIndex >= parts.Length)
        {
            return null;
        }

        var extracted = string.Join(rule.Pattern, parts[startIndex..(endIndex + 1)]);

        return string.IsNullOrWhiteSpace(extracted) ? null : extracted;
    }
}
