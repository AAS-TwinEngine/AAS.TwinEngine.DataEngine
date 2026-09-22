using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients.Caching;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Options;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.SubmodelRegistryProvider.Services;

public class SubmodelDescriptorProvider(ILogger<SubmodelDescriptorProvider> logger, IOptions<TemplateManagementConfig> options, ICachedGetRequestClient cachedHttp) : ISubmodelDescriptorProvider
{
    private const string SubModelRegistryPath = ApiPaths.SubmodelDescriptors;
    private readonly TemplateManagementConfig _config = options.Value;

    public async Task<SubmodelDescriptor> GetDataForSubmodelDescriptorByIdAsync(string id, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetSubmodelDescriptorTemplate, DataEngineTracing.Attributes.TemplateId, id);

        var encodedAasId = id.EncodeBase64Url();

        var url = $"/{SubModelRegistryPath}/{encodedAasId}";

        var responseContent = await cachedHttp.GetStringAsync(url, HttpClientNames.SubmodelRegistry, _config.SubmodelTemplateRegistry.LocalCacheExpirationInMinutes, cancellationToken).ConfigureAwait(false);

        try
        {
            var jsonNode = JsonNode.Parse(responseContent);
            var descriptorNode = jsonNode?["result"] ?? jsonNode;
            var descriptor = DeserializeSubmodelDescriptor(descriptorNode);

            if (descriptor is not null)
            {
                return descriptor;
            }

            logger.LogError("Failed to deserialize the submodel descriptor. Submodel ID: {SubmodelId}", id);
            throw new ResponseParsingException();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize SubmodelDescriptor from response. Submodel ID: {SubmodelId}, Response: {ResponseContent}", id, responseContent);
            throw new ResponseParsingException();
        }
    }

    private static SubmodelDescriptor? DeserializeSubmodelDescriptor(JsonNode? descriptorNode)
    {
        if (descriptorNode is null)
        {
            return null;
        }

        return new SubmodelDescriptor
        {
            Description = AasJsonNodeDeserializer.DeserializeAasArray(descriptorNode["description"], Jsonization.Deserialize.LangStringTextTypeFrom),
            DisplayName = AasJsonNodeDeserializer.DeserializeAasArray(descriptorNode["displayName"], Jsonization.Deserialize.LangStringNameTypeFrom),
            Extensions = AasJsonNodeDeserializer.DeserializeAasArray(descriptorNode["extensions"], Jsonization.Deserialize.ExtensionFrom),
            Administration = AasJsonNodeDeserializer.DeserializeAasNode(descriptorNode["administration"], Jsonization.Deserialize.AdministrativeInformationFrom),
            IdShort = descriptorNode["idShort"]?.GetValue<string>(),
            Id = descriptorNode["id"]?.GetValue<string>(),
            SemanticId = AasJsonNodeDeserializer.DeserializeAasNode(descriptorNode["semanticId"], Jsonization.Deserialize.ReferenceFrom),
            SupplementalSemanticId = AasJsonNodeDeserializer.DeserializeAasArray(descriptorNode["supplementalSemanticId"], Jsonization.Deserialize.ReferenceFrom),
            Endpoints = descriptorNode["endpoints"]?.Deserialize<List<EndpointData>>()
        };
    }
}
