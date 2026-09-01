using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
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

    public async Task<SubmodelDescriptors> GetAllSubmodelDescriptorsAsync(int limit, string? cursor, CancellationToken cancellationToken)
    {
        var pageSize = limit;
        var paginationResult = await CollectSubmodelDescriptorPageAsync(pageSize, cursor, cancellationToken).ConfigureAwait(false);
        var submodelIds = paginationResult.SubmodelIds;

        var allDescriptors = await BuildSubmodelDescriptorsAsync(submodelIds, cancellationToken).ConfigureAwait(false);

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

    private async Task<List<SubmodelDescriptor>> BuildSubmodelDescriptorsAsync(List<string> submodelIds, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(_concurrentOperationsLimit, _concurrentOperationsLimit);
        var tasks = new Task<SubmodelDescriptor?>[submodelIds.Count];

        for (var i = 0; i < submodelIds.Count; i++)
        {
            tasks[i] = BuildSingleSubmodelDescriptorAsync(submodelIds[i], semaphore, cancellationToken);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var descriptors = new List<SubmodelDescriptor>(results.Length);
        descriptors.AddRange(results.Where(result => result is not null)!);

        return descriptors;
    }

    private async Task<SubmodelDescriptor?> BuildSingleSubmodelDescriptorAsync(string submodelId, SemaphoreSlim semaphore, CancellationToken cancellationToken)
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
    }

    private async Task<SubmodelPageResult> CollectSubmodelDescriptorPageAsync(int pageSize, string? encodedCursor, CancellationToken cancellationToken)
    {
        var incomingCursor = SubmodelPaginationCursor.Decode(encodedCursor);
        if (incomingCursor is null && !string.IsNullOrWhiteSpace(encodedCursor))
        {
            throw new InvalidUserInputException();
        }
        var state = new SubmodelPaginationState(incomingCursor, pageSize);
        var pluginCursor = state.TrackingAasId;

        while (state.CollectedIds.Count < pageSize)
        {
            var shellsResult = await aasRepositoryService.GetShellsByFiltersAsync(null, pageSize, pluginCursor?.EncodeBase64Url(), cancellationToken).ConfigureAwait(false);

            var shellList = shellsResult?.Result?.Where(s => !string.IsNullOrWhiteSpace(s.Id)).ToList() ?? [];

            if (shellList.Count == 0)
            {
                break;
            }

            var limitReached = ProcessShellBatch(shellList, pageSize, state);

            if (limitReached)
            {
                break;
            }

            if (shellsResult.PagingMetaData?.Cursor is null)
            {
                break;
            }

            pluginCursor = state.TrackingAasId;
        }

        return new SubmodelPageResult(state.CollectedIds, state.BuildNextCursor(pageSize));
    }

    private static bool ProcessShellBatch(List<AasCore.Aas3_1.IAssetAdministrationShell> shellList, int pageSize, SubmodelPaginationState state)
    {
        foreach (var shell in shellList)
        {
            var submodelIds = GetSubmodelIdsForShell(shell);

            if (state.CollectSubmodelIds(submodelIds, shell.Id, pageSize))
            {
                return true;
            }
        }

        return false;
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

    private string GenerateHref(string encodedId) => $"{_baseUrl}{ApiPaths.Submodels}/{encodedId}";
}
