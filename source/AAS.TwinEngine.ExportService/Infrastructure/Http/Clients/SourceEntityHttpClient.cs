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
/// plain JSON arrays and paged responses shaped as <c>{ "paging_metadata": { "cursor": ... }, "result": [...] }</c>,
/// following cursors until the source reports no continuation.
/// </summary>
public sealed class SourceEntityHttpClient : ISourceEntityReader
{
    // Hard cap to prevent runaway loops if a broken source keeps returning the same cursor.
    private const int MaxPagesPerRead = 10_000;

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
            var entities = new List<SourceEntity>();
            string? cursor = null;
            var page = 0;
            var seenCursors = new HashSet<string>(StringComparer.Ordinal);

            do
            {
                var requestUri = BuildPageUri(endpoint.Path, cursor);

                using var response = await client.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new SourceUnavailableException(
                        $"Source read for {kind} failed with status {(int)response.StatusCode}.");
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var (pageEntities, nextCursor) = ParsePage(json);
                entities.AddRange(pageEntities);

                if (!string.IsNullOrEmpty(nextCursor) && !seenCursors.Add(nextCursor))
                {
                    _logger.LogWarning(
                        "Source for {EntityKind} returned a repeated pagination cursor. Stopping to avoid a loop.",
                        kind);
                    break;
                }

                cursor = nextCursor;
                page++;
            }
            while (!string.IsNullOrEmpty(cursor) && page < MaxPagesPerRead);

            if (page >= MaxPagesPerRead && !string.IsNullOrEmpty(cursor))
            {
                throw new SourceUnavailableException(
                    $"Source read for {kind} exceeded the {MaxPagesPerRead}-page safety cap.");
            }

            return entities;
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

    private static string BuildPageUri(string basePath, string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
        {
            return basePath;
        }

        var encoded = Uri.EscapeDataString(cursor);
        var separator = basePath.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{basePath}{separator}cursor={encoded}";
    }

    private (IReadOnlyList<SourceEntity> Entities, string? NextCursor) ParsePage(string json)
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
            throw new SourceUnavailableException(
                "Source response was not a JSON array or paged wrapper.");
        }

        var entities = new List<SourceEntity>();
        foreach (var element in array.EnumerateArray())
        {
            var identifier = ExtractIdentifier(element);
            if (string.IsNullOrEmpty(identifier))
            {
                throw new SourceUnavailableException("Source entity is missing its 'id' property.");
            }

            var raw = element.GetRawText();
            entities.Add(new SourceEntity(identifier, raw));
        }

        return (entities, ExtractNextCursor(root));
    }

    private static string? ExtractNextCursor(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!root.TryGetProperty("paging_metadata", out var paging) || paging.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!paging.TryGetProperty("cursor", out var cursorProp) || cursorProp.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var cursor = cursorProp.GetString();
        return string.IsNullOrEmpty(cursor) ? null : cursor;
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

