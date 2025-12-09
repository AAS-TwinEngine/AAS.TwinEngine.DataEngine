using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;

public class ShellDescriptorDataHandler(ILogger<ShellDescriptorDataHandler> logger) : IShellDescriptorDataHandler
{
    public IList<ShellDescriptor> FillOut(ShellDescriptor template, IList<ShellDescriptorMetaData> metaData)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(metaData);

        return metaData
               .Select(value =>
               {
                   var clonedDescriptor = Clone(template);
                   return FillOut(clonedDescriptor, value);
               })
               .ToList();
    }

    public ShellDescriptor FillOut(ShellDescriptor template, ShellDescriptorMetaData metaData)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(metaData);

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
        descriptor.SpecificAssetIds = metaData.SpecificAssetIds;
    }

    private ShellDescriptor Clone(ShellDescriptor shellDescriptor)
    {
        ArgumentNullException.ThrowIfNull(shellDescriptor);

        try
        {
            var content = JsonSerializer.Serialize(shellDescriptor, JsonSerializationOptions.Serialization);

            var cloned = JsonSerializer.Deserialize<ShellDescriptor>(content, JsonSerializationOptions.Serialization)
                         ?? throw new InternalDataProcessingException();

            return cloned;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while cloning ShellDescriptor. Object: {@ShellDescriptor}", shellDescriptor);
            throw new InternalDataProcessingException();
        }
    }
}

