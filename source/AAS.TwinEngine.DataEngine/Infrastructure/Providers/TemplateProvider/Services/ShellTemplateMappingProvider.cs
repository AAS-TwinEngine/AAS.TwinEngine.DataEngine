using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

using DataEngineTracing = AAS.TwinEngine.DataEngine.ApplicationLogic.Observability.DataEngineTracing;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

public class ShellTemplateMappingProvider(ILogger<ShellTemplateMappingProvider> logger, IOptions<TemplateManagementConfig> options) : IShellTemplateMappingProvider
{
    private sealed record ProductIdLookup(string? Value);

    private readonly ILogger<ShellTemplateMappingProvider> _logger = logger ?? throw new InvalidDependencyException(nameof(logger), logger);
    private readonly IList<ShellTemplateMappings> _shellTemplateMappings = options.Value.TemplateMappingRules.ShellTemplateMappings ?? throw new InvalidDependencyException(nameof(options.Value.TemplateMappingRules.ShellTemplateMappings), logger);
    private readonly IList<HashSet<string>> _shellTemplateAllowlists = options.Value.TemplateMappingRules.ShellTemplateMappings
        ?.Select(mapping => mapping.Allowlist
            .SelectMany(entry => entry.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase))
        .ToList() ?? throw new InvalidDependencyException(nameof(options.Value.TemplateMappingRules.ShellTemplateMappings), logger);
    private readonly IList<AasIdExtractionRule> _aasIdExtractionRules = options.Value.TemplateMappingRules.AasIdExtractionRules ?? throw new InvalidDependencyException(nameof(options.Value.TemplateMappingRules.AasIdExtractionRules), logger);
    private readonly ConcurrentDictionary<string, ProductIdLookup> _productIdCache = new(StringComparer.Ordinal);
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public string? GetTemplateId(string aasIdentifier)
    {
        ArgumentNullException.ThrowIfNull(aasIdentifier);

        var productId = GetCachedProductId(aasIdentifier);

        string? templateId = null;
        for (var index = 0; index < _shellTemplateMappings.Count; index++)
        {
            var mapping = _shellTemplateMappings[index];
            var allowlist = _shellTemplateAllowlists[index];
            var isAllowlisted = productId is not null && MatchesAllowlist(allowlist, productId);

            if (isAllowlisted || mapping.Pattern.Any(pattern => Regex.IsMatch(aasIdentifier, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, _regexTimeout)))
            {
                templateId = mapping.TemplateId;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            _logger.LogError("No matching template found for shell: {AasIdentifier}", aasIdentifier);
            throw new ResourceNotFoundException();
        }

        return templateId;
    }

    private static bool MatchesAllowlist(HashSet<string> allowlist, string identifier)
    {
        if (allowlist.Contains(identifier))
        {
            return true;
        }

        return allowlist.Any(allowedIdentifier => identifier.StartsWith(allowedIdentifier + "-", StringComparison.OrdinalIgnoreCase));
    }

    public string GetProductIdFromRule(string aasIdentifier)
    {
        ArgumentNullException.ThrowIfNull(aasIdentifier);

        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetProductId, DataEngineTracing.Attributes.ShellId, aasIdentifier);
        var productId = GetCachedProductId(aasIdentifier);

        if (productId is not null)
        {
            _logger.LogInformation("Successfully extracted ProductId: {ProductId}", productId);
            return productId;
        }

        _logger.LogError("ProductId could not be extracted from the provided aas Identifier.");
        throw new ResourceNotFoundException();
    }

    private string? GetCachedProductId(string aasIdentifier) =>
        _productIdCache.GetOrAdd(aasIdentifier, identifier => new ProductIdLookup(TryGetProductIdFromRules(identifier))).Value;

    private string? TryGetProductIdFromRules(string aasIdentifier)
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

            return extracted;
        }

        return null;
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
