using System.Collections.Concurrent;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
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

    public async Task<SubmodelDescriptors> GetAllSubmodelDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var pageSize = limit ?? 100;
        var paginationResult = await CollectSubmodelDescriptorPageAsync(pageSize, cursor, cancellationToken).ConfigureAwait(false);
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

    private async Task<SubmodelDescriptorPageResult> CollectSubmodelDescriptorPageAsync(int pageSize, string? encodedCursor, CancellationToken cancellationToken)
    {
        var incomingCursor = SubmodelPaginationCursor.Decode(encodedCursor);
        var state = new PaginationState(incomingCursor);
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

        var nextCursor = state.CollectedIds.Count >= pageSize ? SubmodelPaginationCursor.Encode(state.LastCollectedSubmodelId, state.TrackingAasId) : null;

        return new SubmodelDescriptorPageResult(state.CollectedIds, nextCursor);
    }

    private static bool ProcessShellBatch(List<AasCore.Aas3_1.IAssetAdministrationShell> shellList, int pageSize, PaginationState state)
    {
        foreach (var shell in shellList)
        {
            var submodelIds = GetSubmodelIdsForShell(shell);

            if (submodelIds.Count == 0)
            {
                state.TrackingAasId = shell.Id;
                state.IsFirstAasInResume = false;
                continue;
            }

            var startIndex = 0;

            if (state.IsFirstAasInResume && state.SkipToSubmodelId is not null)
            {
                startIndex = submodelIds.IndexOf(state.SkipToSubmodelId) + 1;
                state.IsFirstAasInResume = false;
                state.SkipToSubmodelId = null;
            }
            else
            {
                state.IsFirstAasInResume = false;
            }

            for (var i = startIndex; i < submodelIds.Count; i++)
            {
                state.CollectedIds.Add(submodelIds[i]);
                state.LastCollectedSubmodelId = submodelIds[i];

                if (state.CollectedIds.Count >= pageSize)
                {
                    if (state.CollectedIds.Contains(submodelIds.Last()))
                    {
                        state.TrackingAasId = shell.Id;
                    }

                    return true;
                }
            }

            state.TrackingAasId = shell.Id;
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

    private sealed record SubmodelDescriptorPageResult(List<string> SubmodelIds, string? NextCursor);

    private sealed class PaginationState(SubmodelPaginationCursor? cursor)
    {
        public List<string> CollectedIds { get; } = [];
        public string? TrackingAasId { get; set; } = cursor?.AasId;
        public string? LastCollectedSubmodelId { get; set; }
        public string? SkipToSubmodelId { get; set; } = cursor?.SubmodelId;
        public bool IsFirstAasInResume { get; set; } = cursor is not null;
    }

    private string GenerateHref(string encodedId) => $"{_baseUrl}{ApiPaths.Submodels}/{encodedId}";
}
