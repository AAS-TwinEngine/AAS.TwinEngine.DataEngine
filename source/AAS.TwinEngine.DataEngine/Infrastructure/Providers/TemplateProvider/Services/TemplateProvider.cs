using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

using AasCore.Aas3_0;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

public class TemplateProvider(ILogger<TemplateProvider> logger, ICreateClient clientFactory, IOptions<AasEnvironmentConfig> aasEnvironment) : ITemplateProvider
{
    private readonly string _subModelRepositoryPath = aasEnvironment.Value.SubModelRepositoryPath;
    private readonly string _aasRegistryPath = aasEnvironment.Value.AasRegistryPath;
    private readonly string _aasRepositoryPath = aasEnvironment.Value.AasRepositoryPath;
    private readonly string _submodelRefPath = aasEnvironment.Value.SubmodelRefPath;
    private readonly string _conceptDescriptionPath = aasEnvironment.Value.ConceptDescriptionPath;

    public async Task<ISubmodel> GetSubmodelTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        var encodedTemplateId = templateId.EncodeBase64Url(logger);

        var url = $"{_subModelRepositoryPath}/{encodedTemplateId}";

        var response = await SendGetRequestAsync(url, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var jsonNode = JsonNode.Parse(content);
            return Jsonization.Deserialize.SubmodelFrom(jsonNode!);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize submodel template JSON. Submodel ID: {SubmodelId}", templateId);
            throw new ResponseParsingException();
        }
    }

    public async Task<ShellDescriptor> GetShellDescriptorsTemplateAsync(CancellationToken cancellationToken)
    {
        var url = $"{_aasRegistryPath}";

        var response = await SendGetRequestAsync(url, AasEnvironmentConfig.AasRegistryHttpClientName, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var document = JsonDocument.Parse(content);

            if (!document.RootElement.TryGetProperty("result", out var resultArray))
            {
                logger.LogWarning("Shell-descriptor JSON does not contain a valid 'result' array.");
                throw new ResourceNotFoundException();
            }

            if (resultArray.GetArrayLength() == 0)
            {
                logger.LogInformation("No shell descriptors found. Returning a manually created template.");
                return ShellDescriptor.CreateDefault();
            }

            var shellDescriptorJson = resultArray[0].GetRawText();
            var descriptor = JsonSerializer.Deserialize<ShellDescriptor>(shellDescriptorJson);
            if (descriptor != null)
            {
                return descriptor;
            }

            logger.LogError("Failed to deserialize the shell descriptor.");
            throw new ResponseParsingException();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize shell descriptor JSON.");
            throw new ResponseParsingException();
        }
    }

    public async Task<IAssetAdministrationShell> GetShellTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        var encodedTemplateId = templateId.EncodeBase64Url(logger);
        var url = $"{_aasRepositoryPath}/{encodedTemplateId}";

        var response = await SendGetRequestAsync(url, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var jsonNode = JsonNode.Parse(content);
            var shell = Jsonization.Deserialize.AssetAdministrationShellFrom(jsonNode!);
            if (shell != null)
            {
                return shell;
            }

            logger.LogError("Failed to deserialize the shell. AasIdentifier: {AasIdentifier}", templateId);
            throw new ResponseParsingException();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize shell JSON. AasIdentifier: {AasIdentifier}", templateId);
            throw new ResponseParsingException();
        }
    }

    public async Task<IAssetInformation> GetAssetInformationTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        var encodedTemplateId = templateId.EncodeBase64Url(logger);
        var url = $"{_aasRepositoryPath}/{encodedTemplateId}/asset-information";

        var response = await SendGetRequestAsync(url, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var jsonNode = JsonNode.Parse(content);
            var assetInformation = Jsonization.Deserialize.AssetInformationFrom(jsonNode!);
            if (assetInformation == null)
            {
                logger.LogError("Failed to deserialize the asset-information. AasIdentifier: {AasIdentifier}", templateId);
                throw new ResponseParsingException();
            }

            return assetInformation;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize asset-information JSON. AasIdentifier: {AasIdentifier}", templateId);
            throw new ResponseParsingException();
        }
    }

    public async Task<List<IReference>> GetSubmodelRefByIdAsync(string templateId, CancellationToken cancellationToken)
    {
        var encodedTemplateId = templateId.EncodeBase64Url(logger);
        var url = $"{_aasRepositoryPath}/{encodedTemplateId}/{_submodelRefPath}";

        var response = await SendGetRequestAsync(url, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var document = JsonDocument.Parse(content);

            if (!document.RootElement.TryGetProperty("result", out var resultElement))
            {
                logger.LogWarning("submodel-ref JSON does not contain a 'result' property.");
                throw new ResourceNotFoundException();
            }

            if (resultElement.ValueKind != JsonValueKind.Array || resultElement.GetArrayLength() == 0)
            {
                logger.LogWarning("submodel-ref 'result' is not a non-empty array.");
                throw new ResourceNotFoundException();
            }

            var references = resultElement.EnumerateArray()
                                          .Select(item => JsonNode.Parse(item.GetRawText()))
                                          .Select(Jsonization.Deserialize.ReferenceFrom!)
                                          .Cast<IReference>().ToList();

            if (references.Count == 0)
            {
                logger.LogError("No valid submodel-refs could be deserialized. AasIdentifier: {AasIdentifier}", templateId);
                throw new ResponseParsingException();
            }

            return references;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize submodel-refs JSON. AasIdentifier: {AasIdentifier}", templateId);
            throw new ResponseParsingException();
        }
    }

    public async Task<IConceptDescription?> GetConceptDescriptionByIdAsync(string cdIdentifier, CancellationToken cancellationToken)
    {
        var encodedCdId = cdIdentifier.EncodeBase64Url(logger);

        var url = $"{_conceptDescriptionPath}/{encodedCdId}";

        try
        {
            var response = await SendGetRequestAsync(url, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var jsonNode = JsonNode.Parse(content);
            return Jsonization.Deserialize.ConceptDescriptionFrom(jsonNode!);
        }
        catch (Exception ex)
        {
            // Intentionally catching all exceptions without rethrowing.
            // Failures in fetching concept descriptions should not break the serialization process.
            // We log the error for observability and return null to allow the caller to continue gracefully.
            logger.LogError(ex, "Failed to fetch or deserialize concept description. CdIdentifier: {CdIdentifier}", cdIdentifier);
            return null;
        }
    }

    private async Task<HttpResponseMessage> SendGetRequestAsync(string url, string httpClientName, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP GET request to {Url}", url);

        var relativeUri = new Uri(url, UriKind.Relative);

        var httpClient = clientFactory.CreateClient(httpClientName);

        var response = await httpClient.GetAsync(relativeUri, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Received successful HTTP response with status code: {StatusCode}", response.StatusCode);
            return response;
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        logger.LogError("Received HTTP GET response with status code: {StatusCode}. Response message: {ResponseMessage}", response.StatusCode, responseContent);

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
}
