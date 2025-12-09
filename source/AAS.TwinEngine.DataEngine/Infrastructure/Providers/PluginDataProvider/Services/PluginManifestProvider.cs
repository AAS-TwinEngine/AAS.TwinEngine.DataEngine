using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginManifestProvider(ILogger<PluginManifestProvider> logger,
                                    IOptions<PluginConfig> plugins,
                                    ICreateClient clientFactory) : IPluginManifestProvider
{
    private const string ManifestEndpoint = "manifest";
    private readonly List<Plugin> _plugins = plugins.Value.Plugins;

    public async Task<IList<PluginManifest>> GetAllPluginManifestsAsync(CancellationToken cancellationToken)
    {
        var manifests = new List<PluginManifest>();
        var relativeUri = new Uri(ManifestEndpoint, UriKind.Relative);

        foreach (var plugin in _plugins)
        {
            using var httpClient = CreateClient($"{PluginConfig.HttpClientNamePrefix}{plugin.PluginName}");
            try
            {
                var response = await httpClient.GetAsync(relativeUri, cancellationToken).ConfigureAwait(false);
                _ = response.EnsureSuccessStatusCode();

                var content = await ProcessResponseAsync(response, ManifestEndpoint, cancellationToken).ConfigureAwait(false);
                var manifestEntity = DeserializeManifest(content);

                manifestEntity.PluginName = plugin.PluginName;
                manifestEntity.PluginUrl = plugin.PluginUrl;

                manifests.Add(manifestEntity);
            }
            catch (TaskCanceledException)
            {
                logger.LogError("Request timed out. Endpoint: {Url}", ManifestEndpoint);
                throw new RequestTimeoutException();
            }
        }

        return manifests;
    }

    private HttpClient CreateClient(string clientName) => clientFactory.CreateClient(clientName);

    private PluginManifest DeserializeManifest(string jsonContent)
    {
        try
        {
            return JsonSerializer.Deserialize<PluginManifest>(jsonContent) ?? throw new ResponseParsingException();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON format in Embedded resource");
            throw new ResponseParsingException();
        }
    }

    private async Task<string> ProcessResponseAsync(HttpResponseMessage response, string url, CancellationToken cancellationToken)
    {
        logger.LogInformation("HTTP request to {Url}", url);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successful response from {Url} with status: {StatusCode}", url, response.StatusCode);
            return responseContent;
        }
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
}
