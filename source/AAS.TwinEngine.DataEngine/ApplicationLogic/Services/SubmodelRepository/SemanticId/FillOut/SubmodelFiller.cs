using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

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

        var submodelElements = submodelTemplate.SubmodelElements.ToList();
        foreach (var submodelElement in submodelElements)
        {
            var semanticId = semanticIdResolver.ExtractSemanticId(submodelElement);

            var matchingNodes = SemanticTreeNavigator.FindBranchNodesBySemanticId(values, semanticId)?.ToList();

            if (matchingNodes == null || matchingNodes.Count == 0)
            {
                continue;
            }

            _ = submodelTemplate.SubmodelElements.Remove(submodelElement);

            if (matchingNodes.Count > 1)
            {
                HandleMultipleMatchingNodes(matchingNodes, submodelElement, submodelTemplate);
            }
            else
            {
                HandleSingleMatchingNode(matchingNodes[0], submodelElement, submodelTemplate);
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

    private void HandleMultipleMatchingNodes(List<SemanticTreeNode> matchingNodes, ISubmodelElement baseElement, ISubmodel submodelTemplate)
    {
        for (var i = 0; i < matchingNodes.Count; i++)
        {
            var node = matchingNodes[i];
            var clonedElement = elementHelper.CloneElement(baseElement);

            if (baseElement is SubmodelElementCollection)
            {
                clonedElement.IdShort = $"{clonedElement.IdShort}{i}";
            }

            _ = FillOutElement(clonedElement, node);
            submodelTemplate.SubmodelElements?.Add(clonedElement);
        }
    }

    private void HandleSingleMatchingNode(SemanticTreeNode node, ISubmodelElement element, ISubmodel submodelTemplate)
    {
        _ = FillOutElement(element, node);
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

        var handler = handlers.FirstOrDefault(h => h.CanHandle(element));
        if (handler == null)
        {
            logger.LogError("InValid submodelElementTemplate Type. IdShort : {IdShort}", element.IdShort);
            throw new InternalDataProcessingException();
        }

        handler.FillOut(element, values, FillOutSubmodelElementValue);
        return element;
    }

    private void FillOutSubmodelElementValue(List<ISubmodelElement> elements, SemanticTreeNode values, bool updateIdShort)
    {
        var originalElements = elements.ToList();
        foreach (var element in originalElements)
        {
            var semanticTreeNodes = GetSemanticNodes(element, values);

            if (semanticTreeNodes == null || semanticTreeNodes.Count == 0)
            {
                continue;
            }

            if (ShouldCloneElements(semanticTreeNodes, element))
            {
                ReplaceWithClones(elements, element, semanticTreeNodes, updateIdShort);
                continue;
            }

            _ = FillOutElement(element, semanticTreeNodes[0]);
        }
    }

    private static bool ShouldCloneElements(List<SemanticTreeNode> nodes, ISubmodelElement element) => nodes.Count > 1 && element is not Property && element is not ReferenceElement;

    private List<SemanticTreeNode>? GetSemanticNodes(ISubmodelElement element, SemanticTreeNode values)
    {
        var semanticId = semanticIdResolver.ExtractSemanticId(element);

        return IsBranchElement(element)
            ? [.. SemanticTreeNavigator.FindNodeBySemanticId<SemanticBranchNode>(values, semanticId).Cast<SemanticTreeNode>()]
            : [.. SemanticTreeNavigator.FindNodeBySemanticId<SemanticLeafNode>(values, semanticId).Cast<SemanticTreeNode>()];
    }

    private static bool IsBranchElement(ISubmodelElement element) => element is not (Property or AasCore.Aas3_0.File or Blob);

    private void ReplaceWithClones(List<ISubmodelElement> elements, ISubmodelElement element, List<SemanticTreeNode> nodes, bool updateIdShort)
    {
        _ = elements.Remove(element);

        for (var i = 0; i < nodes.Count; i++)
        {
            var cloned = elementHelper.CloneElement(element);

            if (updateIdShort)
            {
                cloned.IdShort = $"{cloned.IdShort}{i}";
            }

            _ = FillOutElement(cloned, nodes[i]);
            elements.Add(cloned);
        }
    }
}
