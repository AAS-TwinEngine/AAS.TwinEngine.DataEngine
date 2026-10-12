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
    private readonly ILogger<ShellTemplateMappingProvider> _logger = logger ?? throw new InvalidDependencyException(nameof(logger), logger);
    private readonly IList<ShellTemplateMappings> _shellTemplateMappings = options.Value.TemplateMappingRules.ShellTemplateMappings ?? throw new InvalidDependencyException(nameof(options.Value.TemplateMappingRules.ShellTemplateMappings), logger);
    private readonly bool _hasShellTemplateAllowlists = options.Value.TemplateMappingRules.ShellTemplateMappings.Any(mapping => mapping.Allowlist.Count > 0);
    private readonly IList<HashSet<string>> _shellTemplateAllowlists = options.Value.TemplateMappingRules.ShellTemplateMappings
        ?.Select(mapping => mapping.Allowlist
            .SelectMany(entry => entry.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase))
        .ToList() ?? throw new InvalidDependencyException(nameof(options.Value.TemplateMappingRules.ShellTemplateMappings), logger);
    private readonly IList<AasIdExtractionRule> _aasIdExtractionRules = options.Value.TemplateMappingRules.AasIdExtractionRules ?? throw new InvalidDependencyException(nameof(options.Value.TemplateMappingRules.AasIdExtractionRules), logger);
    private readonly IList<Regex[]> _compiledPatterns = options.Value.TemplateMappingRules.ShellTemplateMappings
        .Select(mapping => mapping.Pattern
            .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2)))
            .ToArray())
        .ToList();
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public string? GetTemplateId(string aasIdentifier)
    {
        ArgumentNullException.ThrowIfNull(aasIdentifier);

        var productId = _hasShellTemplateAllowlists ? TryGetProductIdFromRules(aasIdentifier) : null;

        return GetTemplateId(aasIdentifier, productId);
    }

    /// <inheritdoc />
    public (string TemplateId, string? ProductId) GetTemplateMapping(string aasIdentifier)
    {
        ArgumentNullException.ThrowIfNull(aasIdentifier);

        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetProductId, DataEngineTracing.Attributes.ShellId, aasIdentifier);
        var productId = TryGetProductIdFromRules(aasIdentifier);
        var templateId = GetTemplateId(aasIdentifier, productId);

        return (templateId, productId);
    }

    private string GetTemplateId(string aasIdentifier, string? productId)
    {
        if (productId is null && _hasShellTemplateAllowlists)
        {
            _logger.LogWarning("Shell template allowlists cannot be evaluated for shell: {AasIdentifier}. Check AasIdExtractionRules; no product ID could be extracted.", aasIdentifier);
        }

        string? templateId = null;
        for (var index = 0; index < _shellTemplateMappings.Count; index++)
        {
            var mapping = _shellTemplateMappings[index];
            var allowlist = _shellTemplateAllowlists[index];
            var isAllowlisted = productId is not null && allowlist.Count > 0 && MatchesAllowlist(allowlist, productId);
            // Allowlist and pattern matching are mutually exclusive per mapping; allowlist wins.
            var matchesPattern = mapping.Allowlist.Count == 0 && _compiledPatterns[index].Any(pattern =>
                pattern.IsMatch(aasIdentifier));

            if (isAllowlisted || matchesPattern)
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

        // Prefix matches require a hyphen boundary: "2000-2201" matches "2000-2201-001", not "2000-22010".
        var lookup = allowlist.GetAlternateLookup<ReadOnlySpan<char>>();
        for (var index = 0; index < identifier.Length; index++)
        {
            if (identifier[index] == '-' && lookup.Contains(identifier.AsSpan(0, index)))
            {
                return true;
            }
        }

        return false;
    }

    public string GetProductIdFromRule(string aasIdentifier)
    {
        ArgumentNullException.ThrowIfNull(aasIdentifier);

        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetProductId, DataEngineTracing.Attributes.ShellId, aasIdentifier);
        var productId = TryGetProductIdFromRules(aasIdentifier);

        if (productId is not null)
        {
            _logger.LogInformation("Successfully extracted ProductId: {ProductId}", productId);
            return productId;
        }

        _logger.LogError("ProductId could not be extracted from the provided aas Identifier.");
        throw new ResourceNotFoundException();
    }

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
