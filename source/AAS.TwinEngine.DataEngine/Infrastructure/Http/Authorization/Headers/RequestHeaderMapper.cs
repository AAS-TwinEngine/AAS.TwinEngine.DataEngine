using System.Net.Http.Headers;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;

public class RequestHeaderMapper : IRequestHeaderMapper
{
    private readonly ILogger<RequestHeaderMapper> _logger;
    private readonly IOptions<HeaderForwardingOptions> _options;
    private readonly Regex _headerNameRegex;
    private readonly Regex _headerValueRegex;
    private readonly List<Regex> _blockedPatterns;

    public RequestHeaderMapper(ILogger<RequestHeaderMapper> logger, IOptions<HeaderForwardingOptions> options)
    {
        _logger = logger;
        _options = options;

        var sanitization = options.Value.HeaderSanitization;

        _headerNameRegex = new Regex(sanitization.AllowedCharacters.HeaderNames, RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
        _headerValueRegex = new Regex(sanitization.AllowedCharacters.HeaderValues, RegexOptions.Compiled, TimeSpan.FromMilliseconds(1000));
        _blockedPatterns = sanitization.BlockedPatterns
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
            if (values.Count == 0 || StringValues.IsNullOrEmpty(values))
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
        var mappings = _options.Value.HeaderMappings;
        var allRules = new List<HeaderMappingRule>();

        allRules.AddRange(mappings.TemplateRepository);
        allRules.AddRange(mappings.TemplateRegistry);

        foreach (var pluginMappings in mappings.Plugins.Values)
        {
            allRules.AddRange(pluginMappings);
        }

        return allRules;
    }

    public void ApplyMappings(HttpContext? httpContext, HttpRequestMessage outgoingRequest, string clientName)
    {
        ArgumentNullException.ThrowIfNull(outgoingRequest);
        ArgumentNullException.ThrowIfNull(clientName);

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

            if (!httpContext.Request.Headers.TryGetValue(sourceName, out var values) || values.Count == 0 || StringValues.IsNullOrEmpty(values))
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

    private static bool IsRuleValid(HeaderMappingRule rule) => !string.IsNullOrWhiteSpace(rule.Source) && !string.IsNullOrWhiteSpace(rule.Target);

    private List<HeaderMappingRule>? ResolveMappingsForClient(string clientName)
    {
        var mappings = _options.Value.HeaderMappings;

        if (string.Equals(clientName, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, StringComparison.OrdinalIgnoreCase))
        {
            return mappings.TemplateRepository;
        }

        if (string.Equals(clientName, AasEnvironmentConfig.AasRegistryHttpClientName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(clientName, AasEnvironmentConfig.SubmodelRegistryHttpClientName, StringComparison.OrdinalIgnoreCase))
        {
            return mappings.TemplateRegistry;
        }

        if (!clientName.StartsWith(PluginConfig.HttpClientNamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var pluginName = clientName[PluginConfig.HttpClientNamePrefix.Length..];

        return mappings.Plugins.TryGetValue(pluginName, out var pluginMappings) ? pluginMappings : null;
    }

    private bool IsHeaderNameValid(string headerName)
    {
        var sanitization = _options.Value.HeaderSanitization;

        return headerName.Length <= sanitization.MaxHeaderNameSize && _headerNameRegex.IsMatch(headerName);
    }

    private bool IsHeaderValueValid(string value)
    {
        var sanitization = _options.Value.HeaderSanitization;

        if (value.Length > sanitization.MaxHeaderSize)
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
