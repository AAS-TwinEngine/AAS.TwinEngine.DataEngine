using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;

public class SubmodelDescriptorService : ISubmodelDescriptorService
{
    private readonly ISubmodelDescriptorProvider _submodelDescriptorProvider;
    private readonly Uri _dataEngineRepositoryBaseUrl;
    private readonly string _subModelRepositoryPath;
    private readonly ISubmodelTemplateMappingProvider _submodelTemplateMappingProvider;

    public SubmodelDescriptorService(
        ISubmodelDescriptorProvider submodelDescriptorProvider,
        ILogger<SubmodelDescriptorService> logger,
        ISubmodelTemplateMappingProvider submodelTemplateMappingProvider,
        IOptions<AasEnvironmentConfig> aasEnvironment)
    {
        _submodelDescriptorProvider = submodelDescriptorProvider;
        _submodelTemplateMappingProvider = submodelTemplateMappingProvider;
        if (aasEnvironment?.Value.DataEngineRepositoryBaseUrl == null)
        {
            logger.LogError("DataEngineRepositoryBaseUrl is missing in AasEnvironmentConfig configuration.");
            throw new ArgumentNullException(nameof(aasEnvironment), "DataEngineRepositoryBaseUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(aasEnvironment.Value.SubModelRepositoryPath))
        {
            logger.LogError("SubModelRepositoryPath is missing in AasEnvironmentConfig configuration.");
            throw new ArgumentNullException(nameof(aasEnvironment), "SubModelRepositoryPath is required.");
        }

        _dataEngineRepositoryBaseUrl = aasEnvironment.Value.DataEngineRepositoryBaseUrl;
        _subModelRepositoryPath = aasEnvironment.Value.SubModelRepositoryPath;
    }

    public async Task<SubmodelDescriptor> GetSubmodelDescriptorByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var templateId = _submodelTemplateMappingProvider.GetTemplateId(id);

            var submodelDescriptorData = await _submodelDescriptorProvider.GetDataForSubmodelDescriptorByIdAsync(templateId, cancellationToken).ConfigureAwait(false);

            SetHref(submodelDescriptorData, id);

            submodelDescriptorData.Id = id;

            return submodelDescriptorData;
        }
        catch (ResourceNotFoundException)
        {
            throw new SubmodelDescriptorNotFoundException(id);
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (RequestTimeoutException ex)
        {
            throw new RegistryNotAvailableException(ex);
        }
        catch (ValidationFailedException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
    }

    private void SetHref(SubmodelDescriptor descriptor, string id)
    {
        var encodedId = id.EncodeBase64Url();
        var href = GenerateHref(encodedId);

        if (descriptor.Endpoints == null || descriptor.Endpoints.Count == 0)
        {
            descriptor.Endpoints =
            [
                new EndpointData
                {
                    ProtocolInformation = new ProtocolInformationData
                    {
                        Href = href
                    }
                }
            ];
            return;
        }

        foreach (var endpoint in descriptor.Endpoints)
        {
            SetHref(endpoint, href);
        }
    }

    private static void SetHref(EndpointData endpoint, string href)
    {
        endpoint.ProtocolInformation ??= new ProtocolInformationData();
        endpoint.ProtocolInformation.Href = href;
    }

    private string GenerateHref(string encodedId) => $"{_dataEngineRepositoryBaseUrl}{_subModelRepositoryPath}/{encodedId}";
}
