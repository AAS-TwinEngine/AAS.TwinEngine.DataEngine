using System.Text.Json;

using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.SubmodelProviders.Helper;

using Microsoft.Extensions.Options;

namespace Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.SubmodelProviders;

public class SubmodelProvider : ISubmodelProvider
{
    private readonly ILogger<SubmodelProvider> _logger;
    private readonly string _submodelElementIndexContextPrefix;
    private Dictionary<string, Dictionary<string, JsonNodeInfo>> _nodeInfoDictionaries;

    public SubmodelProvider(
        ILogger<SubmodelProvider> logger,
        IOptions<Semantics> semantics)
    {
        _logger = logger;
        _submodelElementIndexContextPrefix = semantics.Value.IndexContextPrefix;
        _nodeInfoDictionaries = new Dictionary<string, Dictionary<string, JsonNodeInfo>>(StringComparer.OrdinalIgnoreCase);

        PrecomputeDictionaries();
    }

    private void PrecomputeDictionaries()
    {
        var rootElement = MockData.SubmodelData.RootElement;
        _nodeInfoDictionaries = rootElement
                                .EnumerateObject()
                                .Where(p => p.Value.ValueKind == JsonValueKind.Object)
                                .ToDictionary(keySelector: p => p.Name,
                                              elementSelector: BuildSubmodelDictionary,
                                              comparer: StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, JsonNodeInfo> BuildSubmodelDictionary(JsonProperty product)
    {
        var semanticDictionary = new Dictionary<string, JsonNodeInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in product.Value.EnumerateObject())
        {
            BuildDictionaries(property.Value, property.Name, semanticDictionary);
        }

        return semanticDictionary;
    }

    private static void BuildDictionaries(JsonElement element, string currentPath, Dictionary<string, JsonNodeInfo> dictionary)
    {
        var nodeInfo = new JsonNodeInfo
        {
            Kind = element.ValueKind
        };

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var newPath = string.IsNullOrEmpty(currentPath)
                        ? property.Name
                        : $"{currentPath}.{property.Name}";
                    BuildDictionaries(property.Value, newPath, dictionary);
                }

                break;

            case JsonValueKind.Array:
                nodeInfo.ArrayLength = element.GetArrayLength();
                for (var i = 0; i < element.GetArrayLength(); i++)
                {
                    var newPath = $"{currentPath}.{i}";
                    BuildDictionaries(element[i], newPath, dictionary);
                }

                break;

            case JsonValueKind.String:
                nodeInfo.StringValue = element.GetString()!;
                break;
        }

        dictionary[currentPath] = nodeInfo;
    }

    public SemanticTreeNode EnrichWithData(SemanticTreeNode semanticTreeNode, string submodelId)
    {
        _logger.LogInformation("Starting semantic tree enrichment for submodelId {SubmodelId}", submodelId);

        if (!_nodeInfoDictionaries.TryGetValue(submodelId, out var nodeInfoDictionary))
        {
            _logger.LogError("Submodel identifier {SubmodelId} not found", submodelId);
            throw new NotFoundException();
        }

        var result = FillData(semanticTreeNode, [], nodeInfoDictionary);
        RemoveIndexPrefix(result);

        _logger.LogInformation("Completed tree enrichment");

        return result;
    }

    private SemanticTreeNode FillData(SemanticTreeNode node, List<string> semanticPath, Dictionary<string, JsonNodeInfo> nodeInfoDictionary)
    {
        var newSemanticPath = new List<string>(semanticPath) { node.SemanticId };

        switch (node)
        {
            case SemanticLeafNode leaf:
                return HandleLeafNode(leaf, newSemanticPath, nodeInfoDictionary);
            case SemanticBranchNode branch:
                return HandleBranchNode(branch, newSemanticPath, nodeInfoDictionary);
            default:
                _logger.LogError("Unknown node type: {Type}", node.GetType().Name);
                throw new ArgumentException("Invalid node type");
        }
    }

    private string CreateDictionaryKey(List<string> semanticPath)
    {
        var keyParts = new List<string>();

        foreach (var id in semanticPath)
        {
            if (id.Contains(_submodelElementIndexContextPrefix, StringComparison.Ordinal))
            {
                var parts = id.Split(_submodelElementIndexContextPrefix);
                if (parts.Length == 2 && int.TryParse(parts[1], out var index))
                {
                    keyParts.Add(parts[0]);
                    keyParts.Add(index.ToString());
                    continue;
                }
            }

            keyParts.Add(id);
        }

        return string.Join('.', keyParts);
    }

    private SemanticTreeNode HandleBranchNode(SemanticBranchNode branch, List<string> semanticPath, Dictionary<string, JsonNodeInfo> nodeInfoDictionary)
    {
        var dictKey = CreateDictionaryKey(semanticPath);

        if (!nodeInfoDictionary.TryGetValue(dictKey, out var nodeInfo))
        {
            _logger.LogWarning("Path not found in structure: {Path}", dictKey);
            throw new NotFoundException($"Value Not found or given Element : {dictKey.LastOrDefault()}");
        }

        switch (nodeInfo.Kind)
        {
            case JsonValueKind.Array:
                var clones = CreateArrayNode(branch, dictKey, nodeInfo.ArrayLength, semanticPath, nodeInfoDictionary);
                branch.ReplaceChildren(clones);
                break;
            case JsonValueKind.Object:
                ProcessChildNodes(branch, semanticPath, nodeInfoDictionary);
                break;
            default:
                _logger.LogWarning("Unexpected JSON type {Type} for branch", nodeInfo.Kind);
                break;
        }

        return branch;
    }

    private void ProcessChildNodes(SemanticBranchNode branch, List<string> semanticPath, Dictionary<string, JsonNodeInfo> nodeInfoDictionary)
    {
        var newChildren = new List<SemanticTreeNode>();

        foreach (var child in branch.Children)
        {
            var childPath = new List<string>(semanticPath) { child.SemanticId };
            var childKey = CreateDictionaryKey(childPath);

            if (nodeInfoDictionary.TryGetValue(childKey, out var childInfo) &&
                childInfo.Kind == JsonValueKind.Array &&
                child is SemanticBranchNode childBranch)
            {
                newChildren.AddRange(CreateArrayNode(childBranch, childKey, childInfo.ArrayLength, semanticPath, nodeInfoDictionary));
            }
            else
            {
                var nodes = FillData(child, semanticPath, nodeInfoDictionary);
                newChildren.Add(nodes);
            }
        }

        branch.ReplaceChildren(newChildren);
    }

    private List<SemanticTreeNode> CreateArrayNode(SemanticBranchNode node, string dictKey, int? arrayLength, List<string> parentPath, Dictionary<string, JsonNodeInfo> nodeInfoDictionary)
    {
        if (arrayLength is not > 0)
        {
            _logger.LogWarning("Invalid array length for {Key}", dictKey);
            return [];
        }

        var clones = new List<SemanticTreeNode>();
        for (var i = 0; i < arrayLength.Value; i++)
        {
            var clone = CloneNode(node) as SemanticBranchNode;
            clone!.SemanticId = $"{node.SemanticId}{_submodelElementIndexContextPrefix}{i:D2}";

            var processedClone = FillData(clone, parentPath, nodeInfoDictionary);
            clones.Add(processedClone);
        }

        return clones;
    }

    private SemanticTreeNode HandleLeafNode(SemanticLeafNode leaf, List<string> semanticPath, Dictionary<string, JsonNodeInfo> nodeInfoDictionary)
    {
        var dictKey = CreateDictionaryKey(semanticPath);

        leaf.Value = nodeInfoDictionary.TryGetValue(dictKey, out var nodeInfo) && nodeInfo.StringValue != null
                         ? nodeInfo.StringValue
                         : string.Empty;
        return leaf;
    }

    private static SemanticTreeNode CloneNode(SemanticTreeNode node)
    {
        switch (node)
        {
            case SemanticLeafNode leaf:
                return new SemanticLeafNode(leaf.SemanticId, leaf.DataType, leaf.Value);
            case SemanticBranchNode branch:
                {
                    var cloned = new SemanticBranchNode(branch.SemanticId, branch.DataType);
                    foreach (var child in branch.Children)
                    {
                        var clonedChild = CloneNode(child);
                        cloned.AddChild(clonedChild);
                    }

                    return cloned;
                }
            default:
                throw new InvalidOperationException("Unknown node type");
        }
    }

    private void RemoveIndexPrefix(SemanticTreeNode node)
    {
        var id = node.SemanticId;
        var idx = id.IndexOf(_submodelElementIndexContextPrefix, StringComparison.Ordinal);
        if (idx >= 0)
        {
            node.SemanticId = id[..idx];
        }

        if (node is not SemanticBranchNode branch)
        {
            return;
        }

        foreach (var child in from child in branch.Children
                              where node is SemanticBranchNode
                              select child)
        {
            RemoveIndexPrefix(child);
        }
    }
}
