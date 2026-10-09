using System.Net;
using System.Net.Http.Headers;
using System.Text;

using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;

/// <summary>
/// Applies POST/PUT/DELETE against the configured target endpoint for each entity kind.
/// The URL is <c>{Path}/{Base64URL(identifier)}</c> per the IDTA specification.
/// </summary>
public sealed class TargetEntityHttpClient : ITargetEntityWriter
{
    private const int MaxErrorBodyLength = 2000;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<ExportServiceConfig> _config;
    private readonly ILogger<TargetEntityHttpClient> _logger;

    public TargetEntityHttpClient(
        IHttpClientFactory httpClientFactory,
        IOptions<ExportServiceConfig> config,
        ILogger<TargetEntityHttpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task CreateAsync(EntityKind kind, SourceEntity entity, CancellationToken cancellationToken)
    {
        var endpoint = EndpointResolver.TargetEndpoint(kind, _config.Value.Targets);
        var client = _httpClientFactory.CreateClient(EndpointResolver.TargetClientName(kind));

        using var content = JsonContent(entity.RawJson, _config.Value.Targets.OmitNullProperties);
        using var response = await client.PostAsync(endpoint.Path, content, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning(
                "POST for {EntityKind} {Identifier} returned 409 Conflict. Falling back to PUT.",
                kind, entity.Identifier);
            await UpdateAsync(kind, entity, cancellationToken).ConfigureAwait(false);
            return;
        }

        await EnsureSuccessAsync(response, kind, entity.Identifier, HttpMethod.Post, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EntityKind kind, SourceEntity entity, CancellationToken cancellationToken)
    {
        var endpoint = EndpointResolver.TargetEndpoint(kind, _config.Value.Targets);
        var client = _httpClientFactory.CreateClient(EndpointResolver.TargetClientName(kind));

        var path = BuildItemPath(endpoint.Path, entity.Identifier);
        using var content = JsonContent(entity.RawJson, _config.Value.Targets.OmitNullProperties);
        using var response = await client.PutAsync(path, content, cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, kind, entity.Identifier, HttpMethod.Put, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(EntityKind kind, string identifier, CancellationToken cancellationToken)
    {
        var endpoint = EndpointResolver.TargetEndpoint(kind, _config.Value.Targets);
        var client = _httpClientFactory.CreateClient(EndpointResolver.TargetClientName(kind));

        var path = BuildItemPath(endpoint.Path, identifier);
        using var response = await client.DeleteAsync(path, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation(
                "DELETE for {EntityKind} {Identifier} returned 404. Treating as already gone.",
                kind, identifier);
            return;
        }

        await EnsureSuccessAsync(response, kind, identifier, HttpMethod.Delete, cancellationToken).ConfigureAwait(false);
    }

    private static StringContent JsonContent(string rawJson, bool omitNullProperties)
    {
        var payload = omitNullProperties ? NullPropertyStripper.Strip(rawJson) : rawJson;
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private static string BuildItemPath(string collectionPath, string identifier)
    {
        var encodedId = Base64Url.Encode(identifier);
        return collectionPath.TrimEnd('/') + "/" + encodedId;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        EntityKind kind,
        string identifier,
        HttpMethod method,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await ReadErrorBodyAsync(response, cancellationToken).ConfigureAwait(false);

        throw new HttpRequestException(
            $"Target {method} for {kind} {identifier} failed with status {(int)response.StatusCode}. Response: {body}");
    }

    private static async Task<string> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                return "<empty>";
            }

            return body.Length > MaxErrorBodyLength
                ? body[..MaxErrorBodyLength] + "...<truncated>"
                : body;
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or ObjectDisposedException)
        {
            return "<unreadable>";
        }
    }
}
