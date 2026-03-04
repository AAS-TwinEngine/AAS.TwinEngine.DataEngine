using System.Net.Http.Headers;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Authorization;

public class HeaderMappingService(ILogger<HeaderMappingService> logger, IOptions<HeaderForwardingOptions> options) : IHeaderMappingService
{
    private Regex _headerNameRegex = null!;
    private Regex _headerValueRegex = null!;
    private List<Regex> _blockedPatterns = [];
    private bool _initialized;
    private readonly object _lock = new();

    public void ValidateIncomingHeaders(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return;
        }

        EnsureInitialized();

        foreach (var header in httpContext.Request.Headers)
        {
            var headerName = header.Key;
            var values = header.Value;
            if (values.Count == 0 || StringValues.IsNullOrEmpty(values))
            {
                continue;
            }

            var combinedValue = string.Join(",", values!);

            if (IsHeaderNameValid(headerName) && IsHeaderValueValid(combinedValue))
            {
                continue;
            }

            logger.LogWarning("Incoming header {HeaderName} failed sanitization.", headerName);
            throw new BadRequestException();
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

        EnsureInitialized();

        var mappings = ResolveMappingsForClient(clientName);
        if (mappings == null || mappings.Count == 0)
        {
            return;
        }

        foreach (var rule in mappings)
        {
            if (string.IsNullOrWhiteSpace(rule.Source) || string.IsNullOrWhiteSpace(rule.Target))
            {
                continue;
            }

            var sourceName = rule.Source;
            var targetName = rule.Target;

            var hasHeader = httpContext.Request.Headers.TryGetValue(sourceName, out var values) &&
                            values.Count > 0 && !StringValues.IsNullOrEmpty(values);

            if (!hasHeader)
            {
                if (rule.Required)
                {
                    logger.LogWarning("Required header {HeaderName} is missing for client {ClientName}.", sourceName, clientName);
                    throw new BadRequestException();
                }

                continue;
            }

            if (!IsHeaderNameValid(targetName))
            {
                logger.LogWarning("Target header name {HeaderName} is invalid and will not be forwarded.", targetName);
                if (rule.Required)
                {
                    throw new BadRequestException();
                }

                continue;
            }

            var combinedValue = string.Join(",", [.. values]);

            if (!IsHeaderValueValid(combinedValue))
            {
                logger.LogWarning("Header {HeaderName} for client {ClientName} failed sanitization and will not be forwarded.", sourceName, clientName);
                if (rule.Required)
                {
                    throw new BadRequestException();
                }

                continue;
            }

            try
            {
                if (string.Equals(targetName, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    if (AuthenticationHeaderValue.TryParse(combinedValue, out var authHeader))
                    {
                        outgoingRequest.Headers.Authorization = authHeader;
                    }
                    else if (rule.Required)
                    {
                        throw new BadRequestException();
                    }

                    continue;
                }

                _ = outgoingRequest.Headers.Remove(targetName);
                _ = outgoingRequest.Headers.TryAddWithoutValidation(targetName, combinedValue);
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException)
            {
                logger.LogWarning(ex, "Failed to apply header mapping from {Source} to {Target} for client {ClientName}.", sourceName, targetName, clientName);
                if (rule.Required)
                {
                    throw new BadRequestException();
                }
            }
        }
    }

    private List<HeaderMappingRule>? ResolveMappingsForClient(string clientName)
    {
        var mappings = options.Value.HeaderMappings;

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

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            var sanitization = options.Value.HeaderSanitization;

            _headerNameRegex = new Regex(sanitization.AllowedCharacters.HeaderNames, RegexOptions.Compiled);
            _headerValueRegex = new Regex(sanitization.AllowedCharacters.HeaderValues, RegexOptions.Compiled);

            _blockedPatterns = [];
            foreach (var pattern in sanitization.BlockedPatterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    continue;
                }

                _blockedPatterns.Add(new Regex(pattern, RegexOptions.Compiled));
            }

            _initialized = true;
        }
    }

    private bool IsHeaderNameValid(string headerName)
    {
        var sanitization = options.Value.HeaderSanitization;

        if (headerName.Length > sanitization.MaxHeaderNameSize)
        {
            return false;
        }

        return _headerNameRegex.IsMatch(headerName);
    }

    private bool IsHeaderValueValid(string value)
    {
        var sanitization = options.Value.HeaderSanitization;

        if (value.Length > sanitization.MaxHeaderSize)
        {
            return false;
        }

        if (!_headerValueRegex.IsMatch(value))
        {
            return false;
        }

        foreach (var blocked in _blockedPatterns)
        {
            if (blocked.IsMatch(value))
            {
                return false;
            }
        }

        return true;
    }
}
