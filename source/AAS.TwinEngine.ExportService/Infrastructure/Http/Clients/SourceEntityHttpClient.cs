using System.Text.Json;

using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;

/// <summary>
/// Fetches all entities of a given kind from the configured source endpoint. Handles both
/// plain JSON arrays and paged responses shaped as <c>{ "result": [...] }</c>.
/// </summary>
public sealed class SourceEntityHttpClient : ISourceEntityReader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ExportServiceConfig> _config;
    private readonly ILogger<SourceEntityHttpClient> _logger;

    public SourceEntityHttpClient(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ExportServiceConfig> config,
        ILogger<SourceEntityHttpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SourceEntity>> ReadAllAsync(EntityKind kind, CancellationToken cancellationToken)
    {
        var endpoint = EndpointResolver.SourceEndpoint(kind, _config.CurrentValue.Sources);
        if (!endpoint.Enabled)
        {
            _logger.LogInformation("Source endpoint for {EntityKind} is disabled. Returning empty list.", kind);
            return Array.Empty<SourceEntity>();
        }

        if (string.IsNullOrWhiteSpace(endpoint.Path))
        {
            throw new SourceUnavailableException($"Source endpoint path for {kind} is not configured.");
        }

        var client = _httpClientFactory.CreateClient(EndpointResolver.SourceClientName(kind));

        try
        {
            using var response = await client.GetAsync(endpoint.Path, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new SourceUnavailableException(
                    $"Source read for {kind} failed with status {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return Parse(json);
        }
        catch (SourceUnavailableException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SourceUnavailableException(
                $"Source read for {kind} failed: {ex.Message}", ex);
        }
    }

    private IReadOnlyList<SourceEntity> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var array = root.ValueKind switch
        {
            JsonValueKind.Array => root,
            JsonValueKind.Object when root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array => result,
            _ => default
        };

        if (array.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("Source response was not a JSON array (or paged wrapper). Returning empty list.");
            return Array.Empty<SourceEntity>();
        }

        var entities = new List<SourceEntity>();
        foreach (var element in array.EnumerateArray())
        {
            var identifier = ExtractIdentifier(element);
            if (string.IsNullOrEmpty(identifier))
            {
                _logger.LogWarning("Source entity missing 'id' property. Skipping.");
                continue;
            }

            var raw = element.GetRawText();
            entities.Add(new SourceEntity(identifier, raw));
        }

        return entities;
    }

    private static string? ExtractIdentifier(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (element.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
        {
            return idProp.GetString();
        }

        if (element.TryGetProperty("identification", out var identification) &&
            identification.ValueKind == JsonValueKind.Object &&
            identification.TryGetProperty("id", out var innerId) &&
            innerId.ValueKind == JsonValueKind.String)
        {
            return innerId.GetString();
        }

        return null;
    }
}
