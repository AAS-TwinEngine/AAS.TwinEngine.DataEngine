using System.Text;
using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;

public class AasRegistryProvider(ILogger<AasRegistryProvider> logger, ICreateClient clientFactory, IOptions<AasEnvironmentConfig> aasEnvironment) : IAasRegistryProvider
{
    private readonly string _aasRegistryPath = aasEnvironment.Value.AasRegistryPath;
    private const string HttpClientName = AasEnvironmentConfig.AasRegistryHttpClientName;

    public async Task<List<ShellDescriptor>> GetAllAsync(CancellationToken cancellationToken)
    {
        var url = $"{_aasRegistryPath}";

        var content = await SendGetRequestAndReadContentAsync(url, cancellationToken).ConfigureAwait(false);

        using var doc = JsonDocument.Parse(content);
        if (!doc.RootElement.TryGetProperty("result", out var resultElement))
        {
            logger.LogError("Invalid JSON structure in response.");
            throw new ResponseParsingException();
        }

        return JsonSerializer.Deserialize<List<ShellDescriptor>>(resultElement.GetRawText(), JsonSerializationOptions.Serialization) ?? [];
    }

    public async Task<ShellDescriptor> GetByIdAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        var encodedAasIdentifier = aasIdentifier.EncodeBase64Url();

        var url = $"{_aasRegistryPath}/{encodedAasIdentifier}";

        var content = await SendGetRequestAndReadContentAsync(url, cancellationToken).ConfigureAwait(false);

        return DeserializeContent<ShellDescriptor>(content, "shell descriptor", url);
    }

    public async Task PutAsync(string aasIdentifier, ShellDescriptor shellDescriptorData, CancellationToken cancellationToken)
    {
        var encodedAasIdentifier = aasIdentifier.EncodeBase64Url();

        var url = $"{_aasRegistryPath}/{encodedAasIdentifier}";

        await SendRequestWithBodyAsync(HttpMethod.Put, url, shellDescriptorData, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteByIdAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        var encodedAasIdentifier = aasIdentifier.EncodeBase64Url();

        var url = $"{_aasRegistryPath}/{encodedAasIdentifier}";

        var relativeUri = new Uri(url, UriKind.Relative);

        var client = clientFactory.CreateClient(HttpClientName);

        logger.LogInformation("Sending HTTP DELETE request to {Url}", url);

        var response = await client.DeleteAsync(relativeUri, cancellationToken).ConfigureAwait(false);

        await HandleResponseAsync(response, $"delete ShellDescriptor for {aasIdentifier}", url, cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateAsync(ShellDescriptor shellDescriptorData, CancellationToken cancellationToken) => await SendRequestWithBodyAsync(HttpMethod.Post, _aasRegistryPath, shellDescriptorData, cancellationToken).ConfigureAwait(false);

    private async Task<string> SendGetRequestAndReadContentAsync(string url, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP GET request to {Url}", url);
        var relativeUri = new Uri(url, UriKind.Relative);

        var client = clientFactory.CreateClient(HttpClientName);
        var response = await client.GetAsync(relativeUri, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        logger.LogError("GET failed: {StatusCode}, {Error}", response.StatusCode, error);
        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.NotFound:
                logger.LogError("Requested resource could not be found. Endpoint: {Url}", url);
                throw new ResourceNotFoundException();

            case System.Net.HttpStatusCode.Unauthorized:
            case System.Net.HttpStatusCode.Forbidden:
                logger.LogError("Unauthorized access. Endpoint: {Url}", url);
                throw new ServiceAuthorizationException();

            case System.Net.HttpStatusCode.RequestTimeout:
                logger.LogError("Request timed out. Endpoint: {Url}", url);
                throw new RequestTimeoutException();

            default:
                logger.LogError("Validation error encountered. Endpoint: {Url}", url);
                throw new ValidationFailedException();
        }
    }

    private async Task SendRequestWithBodyAsync(HttpMethod method, string url, ShellDescriptor data, CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient(HttpClientName);
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        logger.LogInformation("Sending HTTP {Method} request to {Url}", method, url);

        using var request = new HttpRequestMessage(method, url);
        request.Content = content;
        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        await HandleResponseAsync(response, $"{method} ShellDescriptor", url, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleResponseAsync(HttpResponseMessage response, string action, string url, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successfully completed action: {Action}", action);
            return;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        logger.LogError("Failed action. Status: {StatusCode}, Response: {Error}", response.StatusCode, error);

        throw response.StatusCode switch
        {
            System.Net.HttpStatusCode.NotFound => new ResourceNotFoundException(),
            System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden =>
                new ServiceAuthorizationException(),
            System.Net.HttpStatusCode.BadRequest => new ValidationFailedException(),
            _ => new RequestTimeoutException()
        };
    }

    private static T DeserializeContent<T>(string content, string context, string url)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(content) ?? throw new InvalidOperationException($"Failed to deserialize {context}.");
        }
        catch
        {
            throw new ResponseParsingException();
        }
    }
}
