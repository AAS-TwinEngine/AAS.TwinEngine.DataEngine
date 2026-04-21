using System.Net.Http.Headers;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;

public class RequestHeaderMapper(
    ILogger<RequestHeaderMapper> logger,
    IOptions<GeneralConfig> generalConfig,
    IOptions<PluginsConfig> pluginsConfig,
    IOptions<TemplateManagementConfig> templateManagementConfig) : IRequestHeaderMapper
{
    private readonly ILogger<RequestHeaderMapper> _logger = logger;
    private readonly HeaderSanitizationOptions _sanitization = generalConfig.Value.HeaderSanitization;
    private readonly PluginsConfig _pluginsConfig = pluginsConfig.Value;
    private readonly TemplateManagementConfig _templateManagementConfig = templateManagementConfig.Value;

    private readonly Regex _headerNameRegex =
        new(generalConfig.Value.HeaderSanitization.AllowedCharacters.HeaderNames, RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));

    private readonly Regex _headerValueRegex =
        new(generalConfig.Value.HeaderSanitization.AllowedCharacters.HeaderValues, RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));

    private readonly List<Regex> _blockedPatterns =
        CreateBlockedPatterns(generalConfig.Value.HeaderSanitization);

    private static List<Regex> CreateBlockedPatterns(HeaderSanitizationOptions sanitization)
    {
        return sanitization.BlockedPatterns
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => new Regex(p, RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000)))
            .ToList();
    }

    public void ValidateIncomingHeaders(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return;
        }

        ValidateHeaderFormats(httpContext);

        ValidateRequiredMappingHeaders(httpContext);
    }

    private void ValidateHeaderFormats(HttpContext httpContext)
    {
        foreach (var (headerName, values) in httpContext.Request.Headers)
        {
            if (StringValues.IsNullOrEmpty(values))
            {
                _logger.LogWarning("Incoming header '{HeaderName}' failed sanitization.", headerName);
                throw new InvalidRequestHeaderException($"Invalid request header: {headerName}");
            }

            var combinedValue = string.Join(",", (IEnumerable<string>)values!);

            if (IsHeaderNameValid(headerName) && IsHeaderValueValid(combinedValue))
            {
                continue;
            }

            _logger.LogWarning("Incoming header '{HeaderName}' failed sanitization.", headerName);
            throw new InvalidRequestHeaderException($"Invalid request header: {headerName}");
        }
    }

    private void ValidateRequiredMappingHeaders(HttpContext httpContext)
    {
        var allRules = GetAllMappingRules();

        foreach (var rule in allRules)
        {
            if (!IsRuleValid(rule) || !rule.Required)
            {
                continue;
            }

            var sourceName = rule.Source!;
            var targetName = rule.Target!;

            var hasHeader = httpContext.Request.Headers.TryGetValue(sourceName, out var values)
                && values.Count > 0
                && !StringValues.IsNullOrEmpty(values);

            if (!hasHeader)
            {
                _logger.LogWarning("Required header '{HeaderName}' is missing.", sourceName);

                throw new InvalidRequestHeaderException($"Required header {sourceName} is missing.");
            }

            if (!IsHeaderNameValid(targetName))
            {
                _logger.LogWarning("Target header name '{HeaderName}' is invalid.", targetName);
                throw new InvalidRequestHeaderException($"Target header name {targetName} is invalid");
            }

            var combinedValue = string.Join(",", [.. values]);
            if (IsHeaderValueValid(combinedValue))
            {
                continue;
            }

            _logger.LogWarning("Header '{HeaderName}' failed sanitization.", sourceName);
            throw new InvalidRequestHeaderException($"Invalid request header: {sourceName}");
        }
    }

    private List<HeaderMappingRule> GetAllMappingRules()
    {
        var allRules = new List<HeaderMappingRule>();

        // Template repository/registry header mappings
        allRules.AddRange(_templateManagementConfig.AasTemplateRepository.HeaderMappings);
        allRules.AddRange(_templateManagementConfig.SubmodelTemplateRepository.HeaderMappings);
        allRules.AddRange(_templateManagementConfig.ConceptDescriptionTemplateRepository.HeaderMappings);
        allRules.AddRange(_templateManagementConfig.AasTemplateRegistry.HeaderMappings);
        allRules.AddRange(_templateManagementConfig.SubmodelTemplateRegistry.HeaderMappings);

        // Plugin header mappings
        foreach (var plugin in _pluginsConfig.Instances)
        {
            allRules.AddRange(plugin.HeaderMappings);
        }

        return allRules;
    }

    public void ApplyMappings(HttpContext? httpContext, HttpRequestMessage outgoingRequest, string clientName)
    {
        ValidateInputs(outgoingRequest, clientName);
        if (httpContext == null)
        {
            return;
        }

        var mappings = ResolveMappingsForClient(clientName);
        if (mappings == null || mappings.Count == 0)
        {
            return;
        }

        foreach (var rule in mappings)
        {
            if (!IsRuleValid(rule))
            {
                continue;
            }

            var sourceName = rule.Source;
            var targetName = rule.Target;

            if (!httpContext.Request.Headers.TryGetValue(sourceName, out var values) || StringValues.IsNullOrEmpty(values))
            {
                continue;
            }

            var combinedValue = string.Join(",", [.. values]);

            if (string.Equals(targetName, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                if (AuthenticationHeaderValue.TryParse(combinedValue, out var authHeader))
                {
                    outgoingRequest.Headers.Authorization = authHeader;
                }
            }
            else
            {
                _ = outgoingRequest.Headers.Remove(targetName);
                _ = outgoingRequest.Headers.TryAddWithoutValidation(targetName, combinedValue);
            }
        }
    }

    private void ValidateInputs(HttpRequestMessage outgoingRequest, string clientName)
    {
        if (outgoingRequest is null)
        {
            throw new InvalidDependencyException(nameof(outgoingRequest), logger);
        }

        if (clientName is null)
        {
            throw new InvalidDependencyException(nameof(clientName), logger);
        }
    }

    private static bool IsRuleValid(HeaderMappingRule rule) => !string.IsNullOrWhiteSpace(rule.Source) && !string.IsNullOrWhiteSpace(rule.Target);

    private IList<HeaderMappingRule>? ResolveMappingsForClient(string clientName)
    {
        if (string.Equals(clientName, HttpClientNames.AasTemplateRepository, StringComparison.OrdinalIgnoreCase))
        {
            return _templateManagementConfig.AasTemplateRepository.HeaderMappings;
        }

        if (string.Equals(clientName, HttpClientNames.SubmodelTemplateRepository, StringComparison.OrdinalIgnoreCase))
        {
            return _templateManagementConfig.SubmodelTemplateRepository.HeaderMappings;
        }

        if (string.Equals(clientName, HttpClientNames.ConceptDescriptorTemplateRepository, StringComparison.OrdinalIgnoreCase))
        {
            return _templateManagementConfig.ConceptDescriptionTemplateRepository.HeaderMappings;
        }

        if (string.Equals(clientName, HttpClientNames.AasRegistry, StringComparison.OrdinalIgnoreCase))
        {
            return _templateManagementConfig.AasTemplateRegistry.HeaderMappings;
        }

        if (string.Equals(clientName, HttpClientNames.SubmodelRegistry, StringComparison.OrdinalIgnoreCase))
        {
            return _templateManagementConfig.SubmodelTemplateRegistry.HeaderMappings;
        }

        if (!clientName.StartsWith(HttpClientNames.PluginDataProviderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var pluginName = clientName[HttpClientNames.PluginDataProviderPrefix.Length..];

        return _pluginsConfig.Instances
            .FirstOrDefault(p => string.Equals(p.Name, pluginName, StringComparison.OrdinalIgnoreCase))
            ?.HeaderMappings;
    }

    private bool IsHeaderNameValid(string headerName) => headerName.Length <= _sanitization.MaxHeaderNameSize && _headerNameRegex.IsMatch(headerName);

    private bool IsHeaderValueValid(string value)
    {
        if (value.Length > _sanitization.MaxHeaderSize)
        {
            return false;
        }

        if (!_headerValueRegex.IsMatch(value))
        {
            return false;
        }

        return !_blockedPatterns.Any(b => b.IsMatch(value));
    }
}
