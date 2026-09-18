using System.Collections.Concurrent;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
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
    // Handler selection depends only on the element type, so the lookup is resolved once per type.
    private readonly ConcurrentDictionary<Type, ISubmodelElementTypeHandler?> _handlersByElementType = new();

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

        var index = SemanticValueIndex.Build(values);
        var submodelElements = submodelTemplate.SubmodelElements.ToList();
        foreach (var submodelElement in submodelElements)
        {
            var semanticId = semanticIdResolver.ExtractSemanticId(submodelElement);

            var matchingNodes = index.GetDirectChildren(values, semanticId);

            if (matchingNodes.Count == 0)
            {
                continue;
            }

            _ = submodelTemplate.SubmodelElements.Remove(submodelElement);

            if (matchingNodes.Count > 1)
            {
                HandleMultipleMatchingNodes(matchingNodes, submodelElement, submodelTemplate, index);
            }
            else
            {
                HandleSingleMatchingNode(matchingNodes[0], submodelElement, submodelTemplate, index);
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

        var internalSemanticIdType = semanticIdResolver.InternalSemanticIdType;

        foreach (var element in elements)
        {
            if (element.Qualifiers is { Count: > 0 } qualifiers)
            {
                _ = qualifiers.RemoveAll(qualifier => qualifier.Type == internalSemanticIdType);
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

    private void HandleMultipleMatchingNodes(IReadOnlyList<SemanticTreeNode> matchingNodes, ISubmodelElement baseElement, ISubmodel submodelTemplate, SemanticValueIndex index)
    {
        var clones = elementHelper.CloneElements(baseElement, matchingNodes.Count);
        var appendIndex = baseElement is SubmodelElementCollection;

        for (var i = 0; i < matchingNodes.Count; i++)
        {
            var clonedElement = clones[i];

            if (appendIndex)
            {
                clonedElement.IdShort = $"{clonedElement.IdShort}{i}";
            }

            _ = FillOutElement(clonedElement, matchingNodes[i], index);
            submodelTemplate.SubmodelElements?.Add(clonedElement);
        }
    }

    private void HandleSingleMatchingNode(SemanticTreeNode node, ISubmodelElement element, ISubmodel submodelTemplate, SemanticValueIndex index)
    {
        _ = FillOutElement(element, node, index);
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

        return FillOutElement(element, values, SemanticValueIndex.Build(values));
    }

    private ISubmodelElement FillOutElement(ISubmodelElement element, SemanticTreeNode values, SemanticValueIndex index)
    {
        var handler = ResolveHandler(element);
        if (handler == null)
        {
            logger.LogError("InValid submodelElementTemplate Type. IdShort : {IdShort}", element.IdShort);
            throw new InternalDataProcessingException();
        }

        handler.FillOut(element, values, (elements, childValues, updateIdShort) => FillOutSubmodelElementValue(elements, childValues, updateIdShort, index));
        return element;
    }

    private ISubmodelElementTypeHandler? ResolveHandler(ISubmodelElement element)
    {
        var elementType = element.GetType();

        if (_handlersByElementType.TryGetValue(elementType, out var cached))
        {
            return cached;
        }

        var handler = handlers.FirstOrDefault(h => h.CanHandle(element));
        _handlersByElementType[elementType] = handler;

        return handler;
    }

    private void FillOutSubmodelElementValue(List<ISubmodelElement> elements, SemanticTreeNode values, bool updateIdShort, SemanticValueIndex index)
    {
        var originalElements = elements.ToList();
        foreach (var element in originalElements)
        {
            var semanticId = semanticIdResolver.ExtractSemanticId(element);
            var semanticTreeNodes = IsBranchElement(element)
                ? index.GetDirectBranchChildren(values, semanticId)
                : index.GetLeafDescendants(values, semanticId);

            if (semanticTreeNodes.Count == 0)
            {
                continue;
            }

            if (ShouldCloneElements(semanticTreeNodes, element))
            {
                ReplaceWithClones(elements, element, semanticTreeNodes, updateIdShort, index);
                continue;
            }

            _ = FillOutElement(element, semanticTreeNodes[0], index);
        }
    }

    private static bool ShouldCloneElements(IReadOnlyList<SemanticTreeNode> nodes, ISubmodelElement element) => nodes.Count > 1 && element is not Property && element is not ReferenceElement;

    private static bool IsBranchElement(ISubmodelElement element) => element is not (Property or AasCore.Aas3_1.File or Blob);

    private void ReplaceWithClones(List<ISubmodelElement> elements, ISubmodelElement element, IReadOnlyList<SemanticTreeNode> nodes, bool updateIdShort, SemanticValueIndex index)
    {
        _ = elements.Remove(element);

        var clones = elementHelper.CloneElements(element, nodes.Count);

        for (var i = 0; i < nodes.Count; i++)
        {
            var cloned = clones[i];

            if (updateIdShort)
            {
                cloned.IdShort = $"{cloned.IdShort}{i}";
            }

            _ = FillOutElement(cloned, nodes[i], index);
            elements.Add(cloned);
        }
    }
}
