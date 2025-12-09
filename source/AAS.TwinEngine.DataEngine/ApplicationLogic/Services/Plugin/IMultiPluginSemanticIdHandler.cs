using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;

public interface IMultiPluginDataHandler
{
    public IDictionary<string, SemanticTreeNode> SplitByPluginManifests(SemanticTreeNode globalTree, IReadOnlyList<PluginManifest> pluginManifests);

    SemanticTreeNode Merge(SemanticTreeNode globalTree, IList<SemanticTreeNode> valueTrees);

    IList<string> GetAvailablePlugins(IReadOnlyList<PluginManifest> manifests, Func<Capabilities, bool> capabilitySelector);
}

