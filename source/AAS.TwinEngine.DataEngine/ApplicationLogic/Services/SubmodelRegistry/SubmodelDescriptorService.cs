using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;

public class SubmodelDescriptorService(
    ISubmodelDescriptorProvider submodelDescriptorProvider,
    ISubmodelTemplateMappingProvider submodelTemplateMappingProvider,
    IAasRepositoryService aasRepositoryService,
    IOptions<GeneralConfig> generalConfig,
    ILogger<SubmodelDescriptorService> logger) : ISubmodelDescriptorService
{
    private readonly Uri _baseUrl = generalConfig.Value.DataEngineRepositoryBaseUrl ?? throw new InvalidDependencyException(nameof(generalConfig.Value.DataEngineRepositoryBaseUrl), logger);

    public async Task<SubmodelDescriptors> GetAllSubmodelDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var shells = await aasRepositoryService.GetShellsByFiltersAsync(null, null, null, cancellationToken).ConfigureAwait(false);
    
        var submodelIds = ExtractDistinctSubmodelIds(shells.Result);
    
        var descriptorTasks = submodelIds.Select(async submodelId =>
        {
            try
            {
                return await GetSubmodelDescriptorByIdAsync(submodelId, cancellationToken).ConfigureAwait(false);
            }
            catch (SubmodelDescriptorNotFoundException ex)
            {
                logger.LogWarning(ex, "Submodel descriptor was not found for submodel id {SubmodelId}. Continuing with remaining descriptors.", submodelId);
                return null;
            }
        });
    
        var allDescriptors = (await Task.WhenAll(descriptorTasks).ConfigureAwait(false))
            .Where(descriptor => descriptor is not null)
            .Cast<SubmodelDescriptor>()
            .ToList();
    
        if (submodelIds.Count > 0 && allDescriptors.Count == 0)
        {
            throw new SubmodelDescriptorNotFoundException();
        }
    
        var (pagedItems, pagingMetaData) = PagingExtensions.GetPagedResult(allDescriptors, d => d.Id!, limit, cursor);
    
        return new SubmodelDescriptors
        {
            PagingMetaData = pagingMetaData,
            Result = pagedItems
        };
    }

    public async Task<SubmodelDescriptor> GetSubmodelDescriptorByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var templateId = submodelTemplateMappingProvider.GetTemplateId(id);

            var submodelDescriptorData = await submodelDescriptorProvider.GetDataForSubmodelDescriptorByIdAsync(templateId, cancellationToken).ConfigureAwait(false);

            SetHref(submodelDescriptorData, id);

            submodelDescriptorData.Id = id;

            return submodelDescriptorData;
        }
        catch (ResourceNotFoundException)
        {
            throw new SubmodelDescriptorNotFoundException(id);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ServiceUnAuthorizedException(ex);
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

    private static IList<string> ExtractDistinctSubmodelIds(IList<AasCore.Aas3_1.IAssetAdministrationShell>? shells)
    {
        return shells?
               .SelectMany(shell => shell.Submodels ?? [])
               .SelectMany(reference => reference.Keys ?? [])
               .Select(key => key.Value)
               .Where(value => !string.IsNullOrWhiteSpace(value))
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToList() ?? [];
    }

    private string GenerateHref(string encodedId) => $"{_baseUrl}{ApiPaths.Submodels}/{encodedId}";
}
