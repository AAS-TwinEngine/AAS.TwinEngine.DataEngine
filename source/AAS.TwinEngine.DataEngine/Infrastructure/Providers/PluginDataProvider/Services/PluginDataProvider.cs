using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using Microsoft.AspNetCore.WebUtilities;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginDataProvider(
    ILogger<PluginDataProvider> logger,
    ICreateClient clientFactory) : IPluginDataProvider
{
    private const string ShellsEndpoint = "shells";
    private const string AssetInformationEndpoint = "assets";
    private const string DataEndpoint = "data";

    public async Task<IList<HttpContent>> GetDataForSemanticIdsAsync(IList<PluginRequestSubmodel> pluginRequests, string submodelId, CancellationToken cancellationToken)
    {
        var url = BuildUrl(DataEndpoint, submodelId.EncodeBase64Url());

        ValidatePluginRequest(pluginRequests, url);

        var relativeUri = new Uri(url, UriKind.Relative);
        var result = new List<HttpContent>();
        foreach (var pluginRequest in pluginRequests)
        {
            using var httpClient = CreateClient(pluginRequest.HttpClientName);
            HttpResponseMessage response;
            try
            {
                response = await httpClient.PostAsync(relativeUri, pluginRequest.JsonSchema, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                logger.LogError("Request timed out. Endpoint: {Url}", url);
                throw new RequestTimeoutException();
            }

            var processedResponse = await ProcessResponseAsync(response, url, cancellationToken).ConfigureAwait(false);
            result.Add(processedResponse);
        }

        return result;
    }

    public async Task<IList<HttpContent>> GetDataForAllShellDescriptorsAsync(int? limit, string? cursor, IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken)
    {
        var result = new List<HttpContent>();
        var exceptions = new List<Exception>();
        var remainingLimit = limit;

        foreach (var pluginRequest in pluginRequests)
        {
            var url = BuildShellsUrl(remainingLimit, cursor);

            var response = await SendPluginRequestAsync(pluginRequest, url, exceptions, cancellationToken);
            if (response == null)
            {
                continue;
            }

            if (response.IsSuccessStatusCode)
            {
                if (remainingLimit.HasValue)
                {
                    var itemsReceived = await CountShellDescriptorsAsync(response.Content).ConfigureAwait(false);
                    remainingLimit -= itemsReceived;

                    if (remainingLimit <= 0)
                    {
                        result.Add(response.Content);
                        break;
                    }

                    if (itemsReceived >= 0 && remainingLimit > 0)
                    {
                        cursor = null;
                    }
                }

                result.Add(response.Content);
                continue;
            }

            exceptions.Add(HandleFailureResponse(response.StatusCode));
        }
        return HandleResultOrThrow(result, exceptions);
    }

    public Task<IList<HttpContent>> GetDataForShellDescriptorByIdAsync(IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken)
        => GetAndProcessAsync(pluginRequests, ShellsEndpoint, cancellationToken);

    public Task<IList<HttpContent>> GetDataForAssetInformationByIdAsync(IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken)
        => GetAndProcessAsync(pluginRequests, AssetInformationEndpoint, cancellationToken);

    private async Task<HttpContent> ProcessResponseAsync(HttpResponseMessage response, string url, CancellationToken cancellationToken)
    {
        logger.LogInformation("HTTP request to {Url}", url);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successful response from {Url} with status: {StatusCode}", url, response.StatusCode);
            return response.Content;
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        logger.LogError("Failed response from {Url}. Status: {StatusCode}. Body: {ResponseContent}", url, response.StatusCode, responseContent);

        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.NotFound:
                logger.LogError("Requested resource could not be found. Endpoint: {Url}", url);
                throw new ResourceNotFoundException();

            case System.Net.HttpStatusCode.Unauthorized:
            case System.Net.HttpStatusCode.Forbidden:
                logger.LogError("Unauthorized access. Endpoint: {Url}", url);
                throw new ServiceAuthorizationException();

            default:
                logger.LogError("Invalid response format. Endpoint: {Url}", url);
                throw new ResponseParsingException();
        }
    }

    private async Task<IList<HttpContent>> GetAndProcessAsync(IList<PluginRequestMetaData> pluginRequests, string path, CancellationToken cancellationToken)
    {
        var result = new List<HttpContent>();
        var exceptions = new List<Exception>();

        foreach (var pluginRequest in pluginRequests)
        {
            var url = BuildUrl(PluginConfig.MetaData, path, pluginRequest.AasIdentifier.EncodeBase64Url());
            var response = await SendPluginRequestAsync(pluginRequest, url, exceptions, cancellationToken);
            if (response == null)
            {
                continue;
            }

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Successful response from {Url} with status: {StatusCode}", url, response.StatusCode);
                result.Add(response.Content);
                continue;
            }

            exceptions.Add(HandleFailureResponse(response.StatusCode));
        }
        return HandleResultOrThrow(result, exceptions);
    }

    private async Task<HttpResponseMessage?> SendPluginRequestAsync(PluginRequestMetaData pluginRequest, string url, IList<Exception> exceptions, CancellationToken cancellationToken)
    {
        if (pluginRequest == null)
        {
            logger.LogWarning("Plugin request is null. Skipping request to {Url}", url);
            exceptions.Add(new ValidationFailedException());
            return null;
        }

        using var httpClient = CreateClient(pluginRequest.HttpClientName);

        try
        {
            return await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            logger.LogError("Request timed out. Endpoint: {Url}", url);
            exceptions.Add(new RequestTimeoutException());
            return null;
        }
    }

    private static async Task<int> CountShellDescriptorsAsync(HttpContent responseContent)
    {
        await using var stream = await responseContent.ReadAsStreamAsync().ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

        if (doc.RootElement.TryGetProperty("result", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
        {
            return itemsElement.GetArrayLength();
        }

        return 0;
    }

    private static string BuildShellsUrl(int? limit, string? cursor)
    {
        const string BaseUrl = $"{PluginConfig.MetaData}/{ShellsEndpoint}";
        var queryParams = new Dictionary<string, string>();

        if (limit is > 0)
        {
            queryParams["limit"] = limit.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            queryParams["cursor"] = cursor;
        }

        return queryParams.Count > 0
                   ? QueryHelpers.AddQueryString(BaseUrl, queryParams!)
                   : BaseUrl;
    }

    private static Exception HandleFailureResponse(System.Net.HttpStatusCode statusCode)
        => statusCode switch
        {
            System.Net.HttpStatusCode.NotFound => new ResourceNotFoundException(),
            System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => new ServiceAuthorizationException(),
            _ => new ResponseParsingException()
        };

    private HttpClient CreateClient(string clientName) => clientFactory.CreateClient(clientName);

    private static string BuildUrl(params object[] segments) => "/" + string.Join("/", segments);

    private void ValidatePluginRequest<T>(T pluginRequest, string url) where T : class
    {
        if (pluginRequest == null)
        {
            logger.LogError("pluginRequest cannot be null or empty for {Url}", url);
            throw new ValidationFailedException();
        }
    }

    private static IList<HttpContent> HandleResultOrThrow(IList<HttpContent> result, IList<Exception> exceptions)
    {
        if (result.Count > 0)
        {
            return result;
        }

        if (exceptions.Count > 0 && exceptions.All(e => e.GetType() == exceptions[0].GetType()))
        {
            throw exceptions[0];
        }

        throw new PluginMetaDataInvalidRequestException();
    }
}
