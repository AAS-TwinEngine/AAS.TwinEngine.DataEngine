using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;
using AAS.TwinEngine.ExportService.ApplicationLogic.Observability;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;

namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Authorization;

/// <summary>
/// Single token provider that supports all configured auth kinds. Endpoint-name → auth
/// config is set up at DI time; tokens are cached in-memory per endpoint.
/// </summary>
public sealed class ConfiguredTokenProvider : ITokenProvider
{
    private readonly IReadOnlyDictionary<string, AuthConfig> _authByEndpoint;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfiguredTokenProvider> _logger;
    private readonly ConcurrentDictionary<string, CachedToken> _cache = new(StringComparer.Ordinal);

    public ConfiguredTokenProvider(
        IReadOnlyDictionary<string, AuthConfig> authByEndpoint,
        IHttpClientFactory httpClientFactory,
        ILogger<ConfiguredTokenProvider> logger)
    {
        _authByEndpoint = authByEndpoint;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> GetTokenAsync(string endpointName, CancellationToken cancellationToken)
    {
        if (!_authByEndpoint.TryGetValue(endpointName, out var auth) || auth.Kind == AuthKind.None)
        {
            return null;
        }

        if (auth.Kind == AuthKind.StaticBearer)
        {
            if (string.IsNullOrWhiteSpace(auth.BearerToken))
            {
                throw new AuthenticationFailedException($"Endpoint '{endpointName}' has StaticBearer auth but no token configured.");
            }

            return auth.BearerToken;
        }

        if (_cache.TryGetValue(endpointName, out var cached) && cached.IsValid)
        {
            return cached.Token;
        }

        var token = await AcquireOAuthTokenAsync(endpointName, auth, cancellationToken).ConfigureAwait(false);
        _cache[endpointName] = token;
        return token.Token;
    }

    private async Task<CachedToken> AcquireOAuthTokenAsync(string endpointName, AuthConfig auth, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(auth.TokenEndpoint) ||
            string.IsNullOrWhiteSpace(auth.ClientId) ||
            string.IsNullOrWhiteSpace(auth.ClientSecret))
        {
            throw new AuthenticationFailedException(
                $"Endpoint '{endpointName}' has OAuthClientCredentials auth but token endpoint, client id or client secret is missing.");
        }

        using var span = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.AcquireToken, "export.endpoint", endpointName);

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", auth.ClientId!),
            new("client_secret", auth.ClientSecret!)
        };

        if (!string.IsNullOrWhiteSpace(auth.Scope))
        {
            form.Add(new("scope", auth.Scope!));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, auth.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _httpClientFactory.CreateClient(HttpClientNames.OAuthToken);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Token acquisition for endpoint '{Endpoint}' failed with status {Status}.",
                    endpointName, (int)response.StatusCode);
                throw new AuthenticationFailedException(
                    $"Token acquisition for endpoint '{endpointName}' failed with status {(int)response.StatusCode}.");
            }

            var payload = await response.Content
                .ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false)
                ?? throw new AuthenticationFailedException($"Token response for endpoint '{endpointName}' was empty.");

            var expiresIn = payload.ExpiresIn > 30 ? payload.ExpiresIn - 30 : payload.ExpiresIn;
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return new CachedToken(payload.AccessToken, expiresAt);
        }
        catch (AuthenticationFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            span.RecordError(ex);
            throw new AuthenticationFailedException(
                $"Token acquisition for endpoint '{endpointName}' failed: {ex.Message}", ex);
        }
    }

    private sealed record CachedToken(string Token, DateTimeOffset ExpiresAt)
    {
        public bool IsValid => DateTimeOffset.UtcNow < ExpiresAt;
    }

    private sealed class OAuthTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 3600;

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";
    }
}
