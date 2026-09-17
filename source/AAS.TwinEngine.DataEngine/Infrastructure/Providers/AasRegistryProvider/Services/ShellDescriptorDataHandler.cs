using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;

public class ShellDescriptorDataHandler(ILogger<ShellDescriptorDataHandler> logger) : IShellDescriptorDataHandler
{
    public ShellDescriptor FillOut(ShellDescriptor template, ShellDescriptorMetaData metaData)
    {
        if (template is null)
        {
            throw new InvalidDependencyException(nameof(template), logger);
        }

        if (metaData is null)
        {
            throw new InvalidDependencyException(nameof(metaData), logger);
        }

        var endpoint = template.Endpoints?.FirstOrDefault();

        if (endpoint?.ProtocolInformation == null)
        {
            logger.LogError("Invalid ShellDescriptor Template: missing endpoint or ProtocolInformation. ShellDescriptorTemplate.Endpoints was {Endpoints}", template.Endpoints);
            throw new InternalDataProcessingException();
        }

        endpoint.ProtocolInformation.Href = metaData.Href;
        UpdateShellDescriptor(template, metaData);

        return template;
    }

    private static void UpdateShellDescriptor(ShellDescriptor descriptor, ShellDescriptorMetaData metaData)
    {
        descriptor.GlobalAssetId = metaData.GlobalAssetId;
        descriptor.IdShort = metaData.IdShort;
        descriptor.Id = metaData.Id;
        if (metaData.ParsedAssetKind.HasValue)
        {
            descriptor.AssetKind = metaData.ParsedAssetKind.Value;
        }

        if (!string.IsNullOrWhiteSpace(metaData.AssetType))
        {
            descriptor.AssetType = metaData.AssetType;
        }

        if (metaData.SpecificAssetIds is not null && metaData.SpecificAssetIds.Count > 0)
        {
            foreach (var specificAssetIdData in metaData.SpecificAssetIds)
            {
                var descriptorAssetId = descriptor.SpecificAssetIds?.FirstOrDefault(x => x.Name == specificAssetIdData.Name);

                descriptorAssetId?.Value = specificAssetIdData.Value;
            }
        }
    }
}

