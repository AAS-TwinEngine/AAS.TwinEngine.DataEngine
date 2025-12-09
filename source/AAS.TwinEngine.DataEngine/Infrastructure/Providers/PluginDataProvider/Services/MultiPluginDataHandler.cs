using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

public class MultiPluginDataHandler(IOptions<Semantics> semantics, ILogger<MultiPluginDataHandler> logger) : IMultiPluginDataHandler
{
    private readonly string _submodelElementIndexContextPrefix = semantics.Value.SubmodelElementIndexContextPrefix;

    public IDictionary<string, SemanticTreeNode> SplitByPluginManifests(SemanticTreeNode globalTree, IReadOnlyList<PluginManifest> pluginManifests)
    {
        var result = new Dictionary<string, SemanticTreeNode>();

        ValidateSemanticIds(globalTree, pluginManifests);

        for (var i = 0; i < pluginManifests.Count; i++)
        {
            var filteredTree = FilterTree(globalTree, pluginManifests[i].SupportedSemanticIds);
            if (filteredTree != null)
            {
                result[pluginManifests[i].PluginName] = filteredTree;
            }
        }

        return result;
    }

    private void ValidateSemanticIds(SemanticTreeNode globalTree, IReadOnlyList<PluginManifest> pluginManifests)
    {
        var allSupportedSemanticIds = pluginManifests
        .SelectMany(p => p.SupportedSemanticIds)
        .Select(GetSemanticId)
        .Distinct()
        .ToHashSet();

        var allSemanticNodes = GetAllSemanticLeafNodes(globalTree);

        var missingSemanticIds = allSemanticNodes
            .Where(node =>
                (node.Cardinality == Cardinality.ZeroToOne ||
                 node.Cardinality == Cardinality.One ||
                 node.Cardinality == Cardinality.ZeroToMany ||
                 node.Cardinality == Cardinality.OneToMany) && !allSupportedSemanticIds.Contains(GetSemanticId(node.SemanticId)))
            .Select(node => node.SemanticId)
            .ToList();

        if (missingSemanticIds.Count > 0)
        {
            var missingList = string.Join(", ", missingSemanticIds);
            logger.LogError("Semantic ID validation failed. The following required SemanticIds are not supported by any plugin manifest: {MissingSemanticIds}", missingList);
            throw new MultiPluginConflictException();
        }
    }

    private static List<SemanticTreeNode> GetAllSemanticLeafNodes(SemanticTreeNode node)
    {
        var nodes = new List<SemanticTreeNode>();

        if (node is SemanticBranchNode branch)
        {
            foreach (var child in branch.Children)
            {
                nodes.AddRange(GetAllSemanticLeafNodes(child));
            }
        }
        else
        {
            nodes.Add(node);
        }

        return nodes;
    }

    private SemanticTreeNode? FilterTree(SemanticTreeNode node, IList<string> supportedSemanticIds)
    {
        var baseSemanticId = GetSemanticId(node.SemanticId);

        return node switch
        {
            SemanticLeafNode leaf => supportedSemanticIds.Contains(baseSemanticId)
                ? new SemanticLeafNode(leaf.SemanticId, leaf.Value, leaf.DataType, leaf.Cardinality) : null,

            SemanticBranchNode branch => FilterBranch(branch, supportedSemanticIds, baseSemanticId),

            _ => null
        };
    }

    private SemanticTreeNode? FilterBranch(SemanticBranchNode branch, IList<string> supportedSemanticIds, string baseSemanticId)
    {
        var newBranch = new SemanticBranchNode(branch.SemanticId, branch.Cardinality);

        foreach (var child in branch.Children)
        {
            if (FilterTree(child, supportedSemanticIds) is { } filteredChild)
            {
                newBranch.AddChild(filteredChild);
            }
        }

        return newBranch.Children.Count > 0 || supportedSemanticIds.Contains(baseSemanticId)
            ? newBranch : null;
    }

    public bool HasIndex(string semanticId) => semanticId.Contains(_submodelElementIndexContextPrefix, StringComparison.OrdinalIgnoreCase);

    public string GetSemanticId(string semanticId)
    {
        if (!HasIndex(semanticId))
        {
            return semanticId;
        }

        var index = semanticId.IndexOf(_submodelElementIndexContextPrefix, StringComparison.OrdinalIgnoreCase);
        return semanticId[..index];
    }

    public SemanticTreeNode Merge(SemanticTreeNode globalTree, IList<SemanticTreeNode> valueTrees)
    {
        return globalTree switch
        {
            SemanticBranchNode branch => MergeBranch(branch, valueTrees),
            SemanticLeafNode leaf => MergeLeaf(leaf, valueTrees),
            _ => throw new InternalDataProcessingException()
        };
    }

    private static SemanticTreeNode MergeBranch(SemanticBranchNode branch, IList<SemanticTreeNode> valueTrees)
    {
        var mergedBranch = new SemanticBranchNode(branch.SemanticId, branch.Cardinality);

        foreach (var child in branch.Children)
        {
            var matchingNodes = FindMatchingNodes(valueTrees, child.SemanticId);

            switch (child)
            {
                case SemanticBranchNode childBranch:
                    var mergedChildren = MergeBranchNodes(childBranch, matchingNodes);
                    foreach (var mc in mergedChildren)
                    {
                        mergedBranch.AddChild(mc);
                    }

                    break;

                case SemanticLeafNode childLeaf:
                    var mergedLeaf = MergeLeafNode(childLeaf, matchingNodes.OfType<SemanticLeafNode>().ToList());
                    if (mergedLeaf != null)
                    {
                        mergedBranch.AddChild(mergedLeaf);
                    }

                    break;
            }
        }

        return mergedBranch;
    }

    private static SemanticTreeNode MergeLeaf(SemanticLeafNode leaf, IList<SemanticTreeNode> valueTrees)
        => MergeLeafNode(leaf, valueTrees.OfType<SemanticLeafNode>().ToList()) ?? throw new InternalDataProcessingException();

    private static List<SemanticTreeNode> MergeBranchNodes(SemanticBranchNode template, List<SemanticTreeNode> candidates)
    {
        var result = new List<SemanticTreeNode>();

        if (candidates.Count == 0)
        {
            result.Add(new SemanticBranchNode(template.SemanticId, template.Cardinality));
            return result;
        }

        var branches = candidates.OfType<SemanticBranchNode>().ToList();

        switch (template.Cardinality)
        {
            case Cardinality.ZeroToOne or Cardinality.One:
                {
                    var merged = CreateMergedBranchFromChildren(branches.SelectMany(b => b.Children), template.SemanticId, template.Cardinality);

                    result.Add(merged);
                    break;
                }

            case Cardinality.ZeroToMany or Cardinality.OneToMany:
            case Cardinality.Unknown when branches.Any(b => IsMany(b.Cardinality)):
                {
                    result.AddRange(branches);
                    break;
                }

            case Cardinality.Unknown:
                {
                    var merged = CreateMergedBranchFromChildren(branches.SelectMany(b => b.Children), template.SemanticId, template.Cardinality);

                    result.Add(merged);
                    break;
                }
        }

        return result;
    }

    private static SemanticLeafNode? MergeLeafNode(SemanticLeafNode template, List<SemanticLeafNode> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        switch (template.Cardinality)
        {
            case Cardinality.ZeroToOne or Cardinality.One:
                {
                    var first = candidates.First();
                    return new SemanticLeafNode(template.SemanticId, first.Value, template.DataType, template.Cardinality);
                }

            case Cardinality.ZeroToMany or Cardinality.OneToMany:
            case Cardinality.Unknown when candidates.Any(c => IsMany(c.Cardinality)):
                {
                    var values = candidates.Select(c => c.Value).ToList();
                    return new SemanticLeafNode(template.SemanticId, values, template.DataType, template.Cardinality);
                }

            case Cardinality.Unknown:
                {
                    var first = candidates.First();
                    return new SemanticLeafNode(template.SemanticId, first.Value, template.DataType, template.Cardinality);
                }

            default:
                return null;
        }
    }

    private static List<SemanticTreeNode> FindMatchingNodes(IEnumerable<SemanticTreeNode> valueTrees, string semanticId)
    {
        var matches = new List<SemanticTreeNode>();

        foreach (var vt in valueTrees.OfType<SemanticBranchNode>())
        {
            var found = vt.Children.Where(c => c.SemanticId.Equals(semanticId, StringComparison.OrdinalIgnoreCase));
            matches.AddRange(found);
        }

        return matches;
    }

    private static bool IsMany(Cardinality cardinality) => cardinality is Cardinality.ZeroToMany or Cardinality.OneToMany;

    private static SemanticBranchNode CreateMergedBranchFromChildren(IEnumerable<SemanticTreeNode> children, string semanticId, Cardinality cardinality)
    {
        var merged = new SemanticBranchNode(semanticId, cardinality);
        foreach (var ch in children)
        {
            merged.AddChild(ch);
        }

        return merged;
    }

    public IList<string> GetAvailablePlugins(IReadOnlyList<PluginManifest> manifests, Func<Capabilities, bool> capabilitySelector)
        => manifests
            .Where(m => capabilitySelector(m.Capabilities))
            .Select(m => m.PluginName)
            .ToList();
}
