using System.Text.Json;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;
using AAS.TwinEngine.Plugin.TestPlugin.Common.Extensions;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.Entity;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.MapperProfiles;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.MetaDataProvider.Helper;

namespace AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.MetaDataProvider;

public class MetaDataProvider : IMetaDataProvider
{
    private readonly ILogger<MetaDataProvider> _logger;
    private readonly Dictionary<string, MetaDataEntity> _shellDescriptorLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AssetData> _assetLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public MetaDataProvider(ILogger<MetaDataProvider> logger)
    {
        _logger = logger;
        BuildDictionaries(MockData.MetaData);
    }

    private void BuildDictionaries(JsonDocument jsonData)
    {
        var entities = jsonData.Deserialize<List<MetaDataEntity>>(_jsonSerializerOptions) ?? [];

        foreach (var entity in entities.Where(e => !string.IsNullOrEmpty(e.Id)))
        {
            _shellDescriptorLookup[entity.Id] = entity;

            if (entity.AssetInformationData is not null)
            {
                _assetLookup[entity.Id] = MapToDomainModel(entity);
            }
        }

        _logger.LogInformation("Loaded {Count} shell-descriptors entries", _shellDescriptorLookup.Count);
        _logger.LogInformation("Loaded {Count} asset entries", _assetLookup.Count);
    }

    private static AssetData MapToDomainModel(MetaDataEntity entity)
    {
        return entity.AssetInformationData!.ToDomainModel(
                                                          entity.GlobalAssetId,
                                                          entity.SpecificAssetIds?.Select(x => new SpecificAssetIdsData
                                                          {
                                                              Name = x.Name,
                                                              Value = x.Value
                                                          }).ToList()
                                                         );
    }

    public Task<ShellDescriptorsData> GetShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken)
    {
        var domainModels = _shellDescriptorLookup.Values.ToList();

        var shellDescriptors = domainModels.ToDomainModelList();

        if (cursor == null || _shellDescriptorLookup.TryGetValue(cursor.DecodeBase64(), out _))
        {
            var (pagedItems, pagingMeta) = Paginator.GetPagedResult(shellDescriptors, s => s.Id!, limit, cursor);

            return Task.FromResult(new ShellDescriptorsData()
            {
                PagingMetaData = pagingMeta,
                Result = pagedItems
            });
        }

        _logger.LogWarning("Invalid cursor provided.");
        throw new NotFoundException(ExceptionMessages.ShellDescriptorDataNotFound);
    }

    public Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        if (_shellDescriptorLookup.TryGetValue(aasIdentifier, out var entity))
        {
            return Task.FromResult(entity.MapToDomainModel());
        }

        _logger.LogWarning("Shell-descriptors not found for ID: {AasIdentifier}", aasIdentifier);
        throw new NotFoundException(ExceptionMessages.ShellDescriptorDataNotFound);
    }

    public Task<AssetData> GetAssetAsync(string shellIdentifier, CancellationToken cancellationToken)
    {
        if (_assetLookup.TryGetValue(shellIdentifier, out var assetInformation))
        {
            return Task.FromResult(assetInformation);
        }

        _logger.LogWarning("Asset not found for ID: {ShellIdentifier}", shellIdentifier);
        throw new NotFoundException(ExceptionMessages.AssetNotFound);
    }
}
