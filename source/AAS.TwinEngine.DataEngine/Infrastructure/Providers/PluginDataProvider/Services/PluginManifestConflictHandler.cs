using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginManifestConflictHandler(
    ILogger<PluginManifestConflictHandler> logger,
    IOptions<MultiPluginConflictOptions> options) : IPluginManifestConflictHandler
{
    private readonly List<PluginManifest> _manifests = [];
    private readonly MultiPluginConflictOptions.MultiPluginConflictOption _handlingMode = options.Value.HandlingMode;

    public IReadOnlyList<PluginManifest> Manifests => _manifests.AsReadOnly();

    public Task ProcessManifests(IList<PluginManifest> manifests)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        if (manifests.Count == 0)
        {
            logger.LogError("No plugin manifests found. ");
            throw new InternalDataProcessingException();
        }

        logger.LogInformation("Started registering the manifest with handling mode : {HandlingMode}. Received {ManifestCount} manifests.", _handlingMode, manifests.Count);

        _manifests.Clear();
        _manifests.AddRange(manifests);
        logger.LogInformation("Initial semantic ID count before processing: {SemanticCount}.", CountTotalSemanticIds());

        ProcessSemanticIds();

        logger.LogInformation("Completed manifest conflict processing. Processed {ManifestCount} manifests.", _manifests.Count);
        logger.LogInformation("Final semantic ID count after processing: {SemanticCount}.", CountTotalSemanticIds());

        return Task.CompletedTask;
    }

    private void ProcessSemanticIds()
    {
        var processedSemanticIds = new HashSet<string>(StringComparer.Ordinal);
        var conflictingSemanticId = new List<string>();

        foreach (var manifest in _manifests)
        {
            var semanticIds = manifest.SupportedSemanticIds;

            if (!semanticIds.Any())
            {
                logger.LogWarning("Plugin {PluginName} contains no supported semantic ids", manifest.PluginName);
                continue;
            }

            var duplicateSemanticIds = semanticIds
                                       .GroupBy(s => s)
                                       .Where(g => g.Count() > 1 || processedSemanticIds.Contains(g.Key))
                                       .Select(g => g.Key);

            conflictingSemanticId.AddRange(duplicateSemanticIds);

            processedSemanticIds.UnionWith(semanticIds);
        }

        conflictingSemanticId.Distinct().ToList().ForEach(HandleConflict);
    }

    private void HandleConflict(string semanticId)
    {
        switch (_handlingMode)
        {
            case MultiPluginConflictOptions.MultiPluginConflictOption.TakeFirst:
                RemoveSemanticIdFromManifest(semanticId);
                break;

            case MultiPluginConflictOptions.MultiPluginConflictOption.SkipConflictingIds:
                RemoveSemanticIdFromAllManifests(semanticId);
                logger.LogInformation("SkipConflictingIds: semantic id '{SemanticId}' removed from all manifests", semanticId);
                break;

            case MultiPluginConflictOptions.MultiPluginConflictOption.ThrowError:
                logger.LogError("Duplicate semantic id {SemanticId} found. Aborting as configured.", semanticId);
                throw new InternalDataProcessingException();
        }
    }

    private void RemoveSemanticIdFromManifest(string semanticIdToRemove)
    {
        var firstManifestsWithSemanticId = _manifests
            .First(m => m?.SupportedSemanticIds != null &&
                        m.SupportedSemanticIds.Contains(semanticIdToRemove));

        RemoveSemanticIdFromAllManifests(semanticIdToRemove);

        firstManifestsWithSemanticId.SupportedSemanticIds.Add(semanticIdToRemove);

        logger.LogInformation("TakeFirst: semantic id {SemanticId} - kept in '{PluginName}' and removed from all other plugins.", semanticIdToRemove, firstManifestsWithSemanticId.PluginName);
    }

    private void RemoveSemanticIdFromAllManifests(string semanticIdToRemove)
    {
        var manifestsWithSemanticId = _manifests
                                      .Where(manifest => manifest?.SupportedSemanticIds != null &&
                                                         manifest.SupportedSemanticIds.Contains(semanticIdToRemove))
                                      .ToList();

        manifestsWithSemanticId.ForEach(m => m.SupportedSemanticIds.Remove(semanticIdToRemove));
    }

    private int CountTotalSemanticIds() => _manifests.Sum(m => m?.SupportedSemanticIds?.Count ?? 0);
}
