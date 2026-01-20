using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.Config;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.ManifestProvider.Helper;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.ManifestProvider;

public class ManifestProvider(
    ILogger<ManifestProvider> logger,
    IOptions<Capabilities> capabilities) : IManifestProvider
{
    private readonly bool _hasShellDescriptor = capabilities.Value.HasShellDescriptor;
    private readonly bool _hasAssetInformation = capabilities.Value.HasAssetInformation;

    public ManifestData GetManifestData()
    {
        logger.LogInformation("Starting getting manifest data");

        var semanticTreeNode = JsonConverter.ParseJson(MockData.SubmodelData);

        var supportedSemanticIds = GetLeafSemanticIds(semanticTreeNode);

        var manifestData = new ManifestData
        {
            SupportedSemanticIds = supportedSemanticIds,
            Capabilities = new CapabilitiesData { HasAssetInformation = _hasAssetInformation, HasShellDescriptor = _hasShellDescriptor }
        };
        return manifestData;
    }

    private static IList<string> GetLeafSemanticIds(SemanticTreeNode node)
    {
        var semanticIds = new List<string>();

        CollectLeafSemanticIds(node, semanticIds);
        return semanticIds.Distinct(StringComparer.Ordinal).ToList();
    }

    private static void CollectLeafSemanticIds(SemanticTreeNode node, List<string> supportedSemanticId)
    {
        if (node is SemanticLeafNode leaf)
        {
            supportedSemanticId.Add(leaf.SemanticId);
            return;
        }

        if (node is not SemanticBranchNode branch)
        {
            return;
        }

        foreach (var child in branch.Children)
        {
            CollectLeafSemanticIds(child, supportedSemanticId);
        }
    }
}
