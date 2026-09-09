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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ExportServiceConfig> _config;
    private readonly ILogger<TargetEntityHttpClient> _logger;

    public TargetEntityHttpClient(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ExportServiceConfig> config,
        ILogger<TargetEntityHttpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task CreateAsync(EntityKind kind, SourceEntity entity, CancellationToken cancellationToken)
    {
        var endpoint = EndpointResolver.TargetEndpoint(kind, _config.CurrentValue.Targets);
        var client = _httpClientFactory.CreateClient(EndpointResolver.TargetClientName(kind));

        using var content = JsonContent(entity.RawJson);
        using var response = await client.PostAsync(endpoint.Path, content, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning(
                "POST for {EntityKind} {Identifier} returned 409 Conflict. Falling back to PUT.",
                kind, entity.Identifier);
            await UpdateAsync(kind, entity, cancellationToken).ConfigureAwait(false);
            return;
        }

        EnsureSuccess(response, kind, entity.Identifier, HttpMethod.Post);
    }

    public async Task UpdateAsync(EntityKind kind, SourceEntity entity, CancellationToken cancellationToken)
    {
        var endpoint = EndpointResolver.TargetEndpoint(kind, _config.CurrentValue.Targets);
        var client = _httpClientFactory.CreateClient(EndpointResolver.TargetClientName(kind));

        var path = BuildItemPath(endpoint.Path, entity.Identifier);
        using var content = JsonContent(entity.RawJson);
        using var response = await client.PutAsync(path, content, cancellationToken).ConfigureAwait(false);

        EnsureSuccess(response, kind, entity.Identifier, HttpMethod.Put);
    }

    public async Task DeleteAsync(EntityKind kind, string identifier, CancellationToken cancellationToken)
    {
        var endpoint = EndpointResolver.TargetEndpoint(kind, _config.CurrentValue.Targets);
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

        EnsureSuccess(response, kind, identifier, HttpMethod.Delete);
    }

    private static StringContent JsonContent(string rawJson)
    {
        var content = new StringContent(rawJson, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private static string BuildItemPath(string collectionPath, string identifier)
    {
        var encodedId = Base64Url.Encode(identifier);
        return collectionPath.TrimEnd('/') + "/" + encodedId;
    }

    private static void EnsureSuccess(HttpResponseMessage response, EntityKind kind, string identifier, HttpMethod method)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new HttpRequestException(
            $"Target {method} for {kind} {identifier} failed with status {(int)response.StatusCode}.");
    }
}
