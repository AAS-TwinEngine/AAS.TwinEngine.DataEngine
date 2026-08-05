using System.Collections.Concurrent;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Pagination;
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
    IOptions<TemplateManagementConfig> templateManagementConfig,
    ILogger<SubmodelDescriptorService> logger) : ISubmodelDescriptorService
{
    private readonly Uri _baseUrl = generalConfig.Value.DataEngineRepositoryBaseUrl ?? throw new InvalidDependencyException(nameof(generalConfig.Value.DataEngineRepositoryBaseUrl), logger);
    private readonly int _concurrentOperationsLimit = templateManagementConfig.Value.SubmodelTemplateRegistry.ConcurrentOperationsLimit;

    public async Task<SubmodelDescriptors> GetAllSubmodelDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var pageSize = limit ?? 100;
        var paginationResult = await SubmodelPaginationHelper.CollectSubmodelPageAsync(
            pageSize,
            cursor,
            async (batchSize, aasCursor, ct) =>
            {
                var shellsResult = await aasRepositoryService.GetShellsByFiltersAsync(null, batchSize, aasCursor?.EncodeBase64Url(), ct).ConfigureAwait(false);
                var items = shellsResult?.Result?.ToList() ?? [];
                return (items, shellsResult?.PagingMetaData?.Cursor);
            },
            shell => shell.Id,
            (shell, _) => Task.FromResult(GetSubmodelIdsForShell(shell)),
            cancellationToken).ConfigureAwait(false);

        var submodelIds = paginationResult.SubmodelIds;

        using var semaphore = new SemaphoreSlim(_concurrentOperationsLimit, _concurrentOperationsLimit);
        var descriptorTasks = submodelIds.Select(async submodelId =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await GetSubmodelDescriptorByIdAsync(submodelId, cancellationToken).ConfigureAwait(false);
            }
            catch (SubmodelDescriptorNotFoundException ex)
            {
                logger.LogWarning(ex, "Submodel descriptor was not found for submodel id {SubmodelId}. Continuing with remaining descriptors.", submodelId);
                return null;
            }
            finally
            {
                _ = semaphore.Release();
            }
        });

        var allDescriptors = (await Task.WhenAll(descriptorTasks).ConfigureAwait(false))
            .Where(descriptor => descriptor is not null)
            .Select(descriptor => descriptor!)
            .ToList();

        if (submodelIds.Count > 0 && allDescriptors.Count == 0)
        {
            throw new SubmodelDescriptorNotFoundException();
        }

        return new SubmodelDescriptors
        {
            PagingMetaData = new PagingMetaData { Cursor = paginationResult.NextCursor },
            Result = allDescriptors
        };
    }

    public async Task<SubmodelDescriptor> GetSubmodelDescriptorByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var templateId = submodelTemplateMappingProvider.GetTemplateId(id);
            var descriptor = await submodelDescriptorProvider.GetDataForSubmodelDescriptorByIdAsync(templateId, cancellationToken).ConfigureAwait(false);

            UpdateEndpointsHref(descriptor, id);

            descriptor.Id = id;

            return descriptor;
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

    private static List<string> GetSubmodelIdsForShell(AasCore.Aas3_1.IAssetAdministrationShell shell)
    {
        return shell.Submodels?
            .SelectMany(reference => reference.Keys ?? [])
            .Select(key => key.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private void UpdateEndpointsHref(SubmodelDescriptor descriptor, string id)
    {
        var encodedId = Base64UrlExtensions.EncodeBase64Url(id);
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

    private string GenerateHref(string encodedId) => $"{_baseUrl}{ApiPaths.Submodels}/{encodedId}";
}
