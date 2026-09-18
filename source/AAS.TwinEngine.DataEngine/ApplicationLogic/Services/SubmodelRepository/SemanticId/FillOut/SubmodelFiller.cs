using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;

public class SubmodelFiller(
    ISemanticIdResolver semanticIdResolver,
    ISubmodelElementHelper elementHelper,
    IEnumerable<ISubmodelElementTypeHandler> handlers,
    ILogger<SubmodelFiller> logger) : ISubmodelFiller
{
    public ISubmodel FillOutTemplate(ISubmodel submodelTemplate, SemanticTreeNode values)
    {
        if (submodelTemplate is null)
        {
            throw new InvalidDependencyException(nameof(submodelTemplate), logger);
        }

        if (submodelTemplate.SubmodelElements is null)
        {
            throw new InvalidDependencyException(nameof(submodelTemplate.SubmodelElements), logger);
        }

        if (values is null)
        {
            throw new InvalidDependencyException(nameof(values), logger);
        }

        var semanticValueIndexes = BuildSemanticValueIndexes(values);
        var submodelElements = submodelTemplate.SubmodelElements.ToList();
        foreach (var submodelElement in submodelElements)
        {
            var semanticId = semanticIdResolver.ExtractSemanticId(submodelElement);

            var matchingNodes = GetDirectSemanticNodes(semanticValueIndexes[values], semanticId);

            if (matchingNodes == null || matchingNodes.Count == 0)
            {
                continue;
            }

            _ = submodelTemplate.SubmodelElements.Remove(submodelElement);

            if (matchingNodes.Count > 1)
            {
                HandleMultipleMatchingNodes(matchingNodes, submodelElement, submodelTemplate, semanticValueIndexes);
            }
            else
            {
                HandleSingleMatchingNode(matchingNodes[0], submodelElement, submodelTemplate, semanticValueIndexes);
            }
        }

        RemoveInternalSemanticIdQualifiers(submodelTemplate.SubmodelElements);

        return submodelTemplate;
    }

    private void RemoveInternalSemanticIdQualifiers(IEnumerable<ISubmodelElement>? elements)
    {
        if (elements == null)
        {
            return;
        }

        foreach (var element in elements)
        {
            if (element.Qualifiers != null)
            {
                var internalQualifiers = element.Qualifiers
                    .Where(q => q.Type == semanticIdResolver.InternalSemanticIdType)
                    .ToList();

                foreach (var qualifier in internalQualifiers)
                {
                    _ = element.Qualifiers.Remove(qualifier);
                }
            }

            switch (element)
            {
                case SubmodelElementCollection collection:
                    RemoveInternalSemanticIdQualifiers(collection.Value);
                    break;
                case SubmodelElementList list:
                    RemoveInternalSemanticIdQualifiers(list.Value);
                    break;
                case Entity entity:
                    RemoveInternalSemanticIdQualifiers(entity.Statements);
                    break;
            }
        }
    }

    private void HandleMultipleMatchingNodes(List<SemanticTreeNode> matchingNodes, ISubmodelElement baseElement, ISubmodel submodelTemplate, IReadOnlyDictionary<SemanticTreeNode, SemanticValueIndex> semanticValueIndexes)
    {
        for (var i = 0; i < matchingNodes.Count; i++)
        {
            var node = matchingNodes[i];
            var clonedElement = elementHelper.CloneElement(baseElement);

            if (baseElement is SubmodelElementCollection)
            {
                clonedElement.IdShort = $"{clonedElement.IdShort}{i}";
            }

            _ = FillOutElement(clonedElement, node, semanticValueIndexes);
            submodelTemplate.SubmodelElements?.Add(clonedElement);
        }
    }

    private void HandleSingleMatchingNode(SemanticTreeNode node, ISubmodelElement element, ISubmodel submodelTemplate, IReadOnlyDictionary<SemanticTreeNode, SemanticValueIndex> semanticValueIndexes)
    {
        _ = FillOutElement(element, node, semanticValueIndexes);
        submodelTemplate.SubmodelElements?.Add(element);
    }

    public ISubmodelElement FillOutElement(ISubmodelElement element, SemanticTreeNode values)
    {
if (element is null)
{
    throw new InvalidDependencyException(nameof(element));
}

if (values is null)
{
    throw new InvalidDependencyException(nameof(values));
}

        return FillOutElement(element, values, BuildSemanticValueIndexes(values));
    }

    private ISubmodelElement FillOutElement(ISubmodelElement element, SemanticTreeNode values, IReadOnlyDictionary<SemanticTreeNode, SemanticValueIndex> semanticValueIndexes)
    {
        var handler = handlers.FirstOrDefault(h => h.CanHandle(element));
        if (handler == null)
        {
            logger.LogError("InValid submodelElementTemplate Type. IdShort : {IdShort}", element.IdShort);
            throw new InternalDataProcessingException();
        }

        handler.FillOut(element, values, (elements, childValues, updateIdShort) => FillOutSubmodelElementValue(elements, childValues, updateIdShort, semanticValueIndexes));
        return element;
    }

    private void FillOutSubmodelElementValue(List<ISubmodelElement> elements, SemanticTreeNode values, bool updateIdShort, IReadOnlyDictionary<SemanticTreeNode, SemanticValueIndex> semanticValueIndexes)
    {
        var originalElements = elements.ToList();
        foreach (var element in originalElements)
        {
            var semanticTreeNodes = GetSemanticNodes(semanticValueIndexes[values], semanticIdResolver.ExtractSemanticId(element), IsBranchElement(element));

            if (semanticTreeNodes == null || semanticTreeNodes.Count == 0)
            {
                continue;
            }

            if (ShouldCloneElements(semanticTreeNodes, element))
            {
                ReplaceWithClones(elements, element, semanticTreeNodes, updateIdShort, semanticValueIndexes);
                continue;
            }

            _ = FillOutElement(element, semanticTreeNodes[0], semanticValueIndexes);
        }
    }

    private static bool ShouldCloneElements(List<SemanticTreeNode> nodes, ISubmodelElement element) => nodes.Count > 1 && element is not Property && element is not ReferenceElement;

    private static List<SemanticTreeNode> GetSemanticNodes(SemanticValueIndex semanticValueIndex, string semanticId, bool branchOnly)
    {
        var index = branchOnly ? semanticValueIndex.DirectChildren : semanticValueIndex.Descendants;
        return index.TryGetValue(semanticId, out var nodes)
            ? [.. nodes.Where(node => !branchOnly || node is SemanticBranchNode).Where(node => branchOnly || node is SemanticLeafNode)]
            : [];
    }

    private static List<SemanticTreeNode> GetDirectSemanticNodes(SemanticValueIndex semanticValueIndex, string semanticId) =>
        semanticValueIndex.DirectChildren.TryGetValue(semanticId, out var nodes) ? [.. nodes] : [];

    private static bool IsBranchElement(ISubmodelElement element) => element is not (Property or AasCore.Aas3_1.File or Blob);

    private void ReplaceWithClones(List<ISubmodelElement> elements, ISubmodelElement element, List<SemanticTreeNode> nodes, bool updateIdShort, IReadOnlyDictionary<SemanticTreeNode, SemanticValueIndex> semanticValueIndexes)
    {
        _ = elements.Remove(element);

        for (var i = 0; i < nodes.Count; i++)
        {
            var cloned = elementHelper.CloneElement(element);

            if (updateIdShort)
            {
                cloned.IdShort = $"{cloned.IdShort}{i}";
            }

            _ = FillOutElement(cloned, nodes[i], semanticValueIndexes);
            elements.Add(cloned);
        }
    }

    private static IReadOnlyDictionary<SemanticTreeNode, SemanticValueIndex> BuildSemanticValueIndexes(SemanticTreeNode values)
    {
        var indexes = new Dictionary<SemanticTreeNode, SemanticValueIndex>();
        BuildSemanticValueIndex(values, indexes);
        return indexes;
    }

    private static Dictionary<string, List<SemanticTreeNode>> BuildSemanticValueIndex(
        SemanticTreeNode node,
        IDictionary<SemanticTreeNode, SemanticValueIndex> indexes)
    {
        var descendants = new Dictionary<string, List<SemanticTreeNode>>(StringComparer.Ordinal) { [node.SemanticId] = [node] };
        var directChildren = new Dictionary<string, List<SemanticTreeNode>>(StringComparer.Ordinal);

        if (node is SemanticBranchNode branch)
        {
            foreach (var child in branch.Children)
            {
                var childIndex = BuildSemanticValueIndex(child, indexes);
                AddNode(directChildren, child);
                foreach (var (semanticId, childNodes) in childIndex)
                {
                    AddNodes(descendants, semanticId, childNodes);
                }
            }
        }

        indexes[node] = new SemanticValueIndex(directChildren, descendants);
        return descendants;
    }

    private static void AddNode(IDictionary<string, List<SemanticTreeNode>> index, SemanticTreeNode node)
    {
        if (!index.TryGetValue(node.SemanticId, out var nodes))
        {
            nodes = [];
            index[node.SemanticId] = nodes;
        }

        nodes.Add(node);
    }

    private static void AddNodes(IDictionary<string, List<SemanticTreeNode>> index, string semanticId, IEnumerable<SemanticTreeNode> nodes)
    {
        if (!index.TryGetValue(semanticId, out var matchingNodes))
        {
            matchingNodes = [];
            index[semanticId] = matchingNodes;
        }

        matchingNodes.AddRange(nodes);
    }

    private sealed record SemanticValueIndex(
        IReadOnlyDictionary<string, List<SemanticTreeNode>> DirectChildren,
        IReadOnlyDictionary<string, List<SemanticTreeNode>> Descendants);
}
