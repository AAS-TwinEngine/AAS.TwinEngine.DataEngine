using System.Net.Http.Headers;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

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

        foreach (var header in httpContext.Request.Headers)
        {
            var headerName = header.Key;
            var values = header.Value;
            if (values.Count == 0 || StringValues.IsNullOrEmpty(values))
            {
                continue;
            }

            var combinedValue = string.Join(",", (IEnumerable<string>)values!);

            if (IsHeaderNameValid(headerName) && IsHeaderValueValid(combinedValue))
            {
                continue;
            }

            _logger.LogWarning("Incoming header failed sanitization.");
            throw new InvalidRequestHeaderException();
        }
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
            ProcessRule(rule, httpContext, outgoingRequest, clientName);
        }
    }

    private void ProcessRule(HeaderMappingRule rule, HttpContext httpContext, HttpRequestMessage outgoingRequest, string clientName)
    {
        if (!IsRuleValid(rule))
        {
            return;
        }

        var sourceName = rule.Source!;
        var targetName = rule.Target!;

        if (!TryGetHeaderValue(httpContext, sourceName, rule.Required, clientName, out var combinedValue))
        {
            return;
        }

        if (!ValidateTargetHeader(targetName, rule.Required))
        {
            return;
        }

        if (!ValidateHeaderValue(combinedValue, sourceName, clientName, rule.Required))
        {
            return;
        }

        ApplyHeader(outgoingRequest, targetName, combinedValue, sourceName, clientName, rule.Required);
    }

    private bool TryGetHeaderValue(HttpContext httpContext, string sourceName, bool required, string clientName, out string combinedValue)
    {
        combinedValue = string.Empty;

        var hasHeader = httpContext.Request.Headers.TryGetValue(sourceName, out var values) && values.Count > 0 && !StringValues.IsNullOrEmpty(values);

        if (!hasHeader)
        {
            if (required)
            {
                _logger.LogWarning("Required header {HeaderName} is missing for client {ClientName}.", sourceName, clientName);

                throw new InvalidRequestHeaderException();
            }

            return false;
        }

        combinedValue = string.Join(",", [.. values]);
        return true;
    }

    private bool ValidateTargetHeader(string targetName, bool required)
    {
        if (IsHeaderNameValid(targetName))
        {
            return true;
        }

        _logger.LogWarning("Target header name {HeaderName} is invalid and will not be forwarded.", targetName);

        return required ? throw new InvalidRequestHeaderException() : false;
    }

    private bool ValidateHeaderValue(string value, string sourceName, string clientName, bool required)
    {
        if (IsHeaderValueValid(value))
        {
            return true;
        }

        _logger.LogWarning("Header {HeaderName} for client {ClientName} failed sanitization and will not be forwarded.", sourceName, clientName);

        return required ? throw new InvalidRequestHeaderException() : false;
    }

    private void ApplyHeader(HttpRequestMessage outgoingRequest, string targetName, string value, string sourceName, string clientName, bool required)
    {
        try
        {
            if (string.Equals(targetName, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                ApplyAuthorizationHeader(outgoingRequest, value, required);
                return;
            }

            _ = outgoingRequest.Headers.Remove(targetName);
            _ = outgoingRequest.Headers.TryAddWithoutValidation(targetName, value);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Failed to apply header mapping from {Source} to {Target} for client {ClientName}.", sourceName, targetName, clientName);

            if (required)
            {
                throw new InvalidRequestHeaderException();
            }
        }
    }

    private static void ApplyAuthorizationHeader(HttpRequestMessage outgoingRequest, string value, bool required)
    {
        if (AuthenticationHeaderValue.TryParse(value, out var authHeader))
        {
            outgoingRequest.Headers.Authorization = authHeader;
            return;
        }

        if (required)
        {
            throw new InvalidRequestHeaderException();
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

        if (clientName.StartsWith(PluginConfig.HttpClientNamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var pluginName = clientName[PluginConfig.HttpClientNamePrefix.Length..];

            if (mappings.Plugins.TryGetValue(pluginName, out var pluginMappings))
            {
                return pluginMappings;
            }
        }

        return null;
    }


    private bool IsHeaderNameValid(string headerName)
    {
        var sanitization = _options.Value.HeaderSanitization;

        if (headerName.Length > sanitization.MaxHeaderNameSize)
        {
            return false;
        }

        return _headerNameRegex.IsMatch(headerName);
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

        if (_blockedPatterns.Any(b => b.IsMatch(value)))
        {
            return false;
        }

        return true;
    }
}
