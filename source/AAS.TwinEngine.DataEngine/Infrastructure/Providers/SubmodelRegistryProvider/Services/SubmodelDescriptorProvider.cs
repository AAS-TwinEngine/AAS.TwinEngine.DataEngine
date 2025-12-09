using System.Net;
using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.SubmodelRegistryProvider.Services;

public class SubmodelDescriptorProvider : ISubmodelDescriptorProvider
{
    private readonly ILogger<SubmodelDescriptorProvider> _logger;
    private readonly ICreateClient _clientFactory;
    private readonly string _subModelRegistryPath;

    public SubmodelDescriptorProvider(
        ILogger<SubmodelDescriptorProvider> logger,
        ICreateClient clientFactory,
        IOptions<AasEnvironmentConfig> aasEnvironment)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        ArgumentException.ThrowIfNullOrWhiteSpace(aasEnvironment?.Value.SubModelRegistryPath, nameof(aasEnvironment));
        _subModelRegistryPath = aasEnvironment.Value.SubModelRegistryPath;
    }

    public async Task<SubmodelDescriptor> GetDataForSubmodelDescriptorByIdAsync(string id, CancellationToken cancellationToken)
    {
        var encodedAasId = id.EncodeBase64Url();

        var url = $"/{_subModelRegistryPath}/{encodedAasId}";

        var relativeUri = new Uri(url, UriKind.Relative);

        var httpClient = _clientFactory.CreateClient(AasEnvironmentConfig.SubmodelRegistryHttpClientName);

        var response = await httpClient.GetAsync(relativeUri, cancellationToken).ConfigureAwait(false);

        var httpResponseContent = await ProcessResponseAsync(response, url, cancellationToken).ConfigureAwait(false);

        var responseContent = await httpResponseContent.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var descriptor = JsonSerializer.Deserialize<SubmodelDescriptor>(responseContent);
            if (descriptor != null)
            {
                return descriptor;
            }

            _logger.LogError("Failed to deserialize the submodel descriptor. Submodel ID: {SubmodelId}", id);
            throw new ResponseParsingException();
        }
        catch (JsonException)
        {
            _logger.LogError("Failed to deserialize SubmodelDescriptor from response. Submodel ID: {SubmodelId}, Response: {ResponseContent}", id, responseContent);
            throw new ResponseParsingException();
        }
    }

    private async Task<HttpContent> ProcessResponseAsync(HttpResponseMessage response, string url, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending HTTP GET request to {Url}", url);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Received successful HTTP response from {Url} with status code: {StatusCode}", url, response.StatusCode);
            return response.Content;
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogError("Received HTTP response from {Url} with status code: {StatusCode}. Response message: {ResponseMessage}", url, response.StatusCode, responseContent);

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                _logger.LogError("Requested resource could not be found. Endpoint: {Url}", url);
                throw new ResourceNotFoundException();

            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                _logger.LogError("Unauthorized access. Endpoint: {Url}", url);
                throw new ServiceAuthorizationException();

            case HttpStatusCode.RequestTimeout:
                _logger.LogError("Request timed out. Endpoint: {Url}", url);
                throw new RequestTimeoutException();

            default:
                _logger.LogError("Validation error encountered. Endpoint: {Url}", url);
                throw new ValidationFailedException();
        }
    }
}
