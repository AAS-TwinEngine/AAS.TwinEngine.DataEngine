using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using File = AasCore.Aas3_0.File;
using Range = AasCore.Aas3_0.Range;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;

public class SubmodelFiller(
    ISemanticIdResolver semanticIdResolver,
    ISubmodelElementHelper elementHelper,
    IReferenceHelper referenceHelper,
    ILogger<SubmodelFiller> logger) : ISubmodelFiller
{

    public ISubmodel FillOutTemplate(ISubmodel submodelTemplate, SemanticTreeNode values)
    {
        ArgumentNullException.ThrowIfNull(submodelTemplate);
        ArgumentNullException.ThrowIfNull(submodelTemplate.SubmodelElements);
        ArgumentNullException.ThrowIfNull(values);

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

        return submodelTemplate;
    }

    private void HandleMultipleMatchingNodes(
        List<SemanticTreeNode> matchingNodes,
        ISubmodelElement baseElement,
        ISubmodel submodelTemplate)
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

    private void HandleSingleMatchingNode(
        SemanticTreeNode node,
        ISubmodelElement element,
        ISubmodel submodelTemplate)
    {
        _ = FillOutElement(element, node);
        submodelTemplate.SubmodelElements?.Add(element);
    }

    private ISubmodelElement FillOutElement(ISubmodelElement submodelElementTemplate, SemanticTreeNode values)
    {
        ArgumentNullException.ThrowIfNull(submodelElementTemplate);
        ArgumentNullException.ThrowIfNull(values);

        switch (submodelElementTemplate)
        {
            case SubmodelElementCollection collection:
                FillOutSubmodelElementCollection(collection, values);
                break;

            case SubmodelElementList list:
                FillOutSubmodelElementList(list, values);
                break;

            case MultiLanguageProperty mlp:
                FillOutMultiLanguageProperty(mlp, values);
                break;

            case Property property:
                FillOutProperty(property, values);
                break;

            case File file:
                FillOutFile(file, values);
                break;

            case Blob blob:
                FillOutBlob(blob, values);
                break;

            case RelationshipElement relationship:
                FillOutRelationshipElement(relationship, values);
                break;

            case ReferenceElement reference:
                FillOutReferenceElement(reference, values);
                break;

            case Range range:
                FillOutRange(range, values);
                break;

            case Entity entity:
                FillOutEntity(entity, values);
                break;

            default:
                logger.LogError("InValid submodelElementTemplate Type. IdShort : {IdShort}", submodelElementTemplate.IdShort);
                throw new InternalDataProcessingException();
        }

        return submodelElementTemplate;
    }

    private void FillOutSubmodelElementList(SubmodelElementList list, SemanticTreeNode values)
    {
        if (list?.Value == null || list.Value.Count == 0)
        {
            return;
        }

        FillOutSubmodelElementValue(list.Value, values, false);
    }

    private void FillOutSubmodelElementCollection(SubmodelElementCollection collection, SemanticTreeNode values)
    {
        if (collection?.Value == null || collection.Value.Count == 0)
        {
            return;
        }

        FillOutSubmodelElementValue(collection.Value, values);
    }

    private void FillOutSubmodelElementValue(List<ISubmodelElement> elements, SemanticTreeNode values, bool updateIdShort = true)
    {
        var originalElements = elements.ToList();
        foreach (var element in originalElements)
        {
            var valueNode = SemanticTreeNavigator.FindNodeBySemanticId(values, semanticIdResolver.ExtractSemanticId(element));
            var semanticTreeNodes = valueNode?.ToList();

            if (semanticTreeNodes == null || semanticTreeNodes.Count == 0)
            {
                continue;
            }

            if (!SemanticTreeNavigator.AreAllNodesOfSameType(semanticTreeNodes, out _))
            {
                logger.LogWarning("Mixed node types found for element '{IdShort}' with SemanticId '{SemanticId}'. Expected all nodes to be either SemanticBranchNode or SemanticLeafNode. Removing element.",
                                  element.IdShort,
                                  semanticIdResolver.ExtractSemanticId(element));
                _ = elements.Remove(element);
                continue;
            }

            if (semanticTreeNodes.Count > 1 && element is not Property && element is not ReferenceElement)
            {
                _ = elements.Remove(element);
                for (var i = 0; i < semanticTreeNodes.Count; i++)
                {
                    var cloned = elementHelper.CloneElement(element);
                    if (updateIdShort)
                    {
                        cloned.IdShort = $"{cloned.IdShort}{i}";
                    }

                    _ = FillOutElement(cloned, semanticTreeNodes[i]);
                    elements.Add(cloned);
                }
            }
            else
            {
                FillOutElement(element, semanticTreeNodes[0]);
            }
        }
    }

    private void FillOutMultiLanguageProperty(MultiLanguageProperty mlp, SemanticTreeNode values)
    {
        var semanticId = semanticIdResolver.ExtractSemanticId(mlp);

        if (SemanticTreeNavigator.FindNodeBySemanticId(values, semanticId).FirstOrDefault() is not SemanticBranchNode valueNode)
        {
            logger.LogInformation("No value node found for MultiLanguageProperty {MlpIdShort}", mlp.IdShort);
            return;
        }

        mlp.Value ??= [];

        var languageValueMap = new Dictionary<string, LangStringTextType>(StringComparer.OrdinalIgnoreCase);
        foreach (var langValue in mlp.Value)
        {
            languageValueMap[langValue.Language] = (LangStringTextType)langValue;
        }

        var languages = elementHelper.ResolveLanguages(mlp);

        var mlpSeparator = semanticIdResolver.MlpPostFixSeparator;
        foreach (var language in languages)
        {
            if (!languageValueMap.TryGetValue(language, out var languageValue))
            {
                languageValue = new LangStringTextType(language, string.Empty);
                mlp.Value.Add(languageValue);
                languageValueMap[language] = languageValue;

                logger.LogInformation("Added language '{Language}' to MultiLanguageProperty {MlpIdShort}", language, mlp.IdShort);
            }

            var languageSemanticId = semanticId + mlpSeparator + language;

            var leafNode = valueNode.Children
                                    .OfType<SemanticLeafNode>()
                                    .FirstOrDefault(child => child.SemanticId.Equals(languageSemanticId, StringComparison.Ordinal));

            if (leafNode != null)
            {
                languageValue.Text = leafNode.Value;
            }
        }
    }

    private void FillOutEntity(Entity entity, SemanticTreeNode values)
    {
        if (entity.EntityType == EntityType.SelfManagedEntity)
        {
            FillOutSelfManagedEntity(entity, values);
        }

        if (entity?.Statements == null || entity.Statements.Count == 0)
        {
            return;
        }

        FillOutSubmodelElementValue(entity.Statements, values);
    }

    private void FillOutSelfManagedEntity(Entity entity, SemanticTreeNode values)
    {
        var semanticId = semanticIdResolver.ResolveElementSemanticId(entity, entity.IdShort!);

        if (SemanticTreeNavigator.FindNodeBySemanticId(values, semanticId).FirstOrDefault() is not SemanticBranchNode valueNode)
        {
            return;
        }

        var globalAssetSemanticId = semanticId + SemanticIdResolver.EntityGlobalAssetIdPostFix;

        var globalAssetNode = valueNode.Children
                                       .OfType<SemanticLeafNode>()
                                       .FirstOrDefault(c => c.SemanticId == globalAssetSemanticId);

        if (globalAssetNode != null)
        {
            entity.GlobalAssetId = globalAssetNode.Value;
        }

        if (entity.SpecificAssetIds != null)
        {
            foreach (var specificAssetId in entity.SpecificAssetIds)
            {
                var specSemanticId = semanticIdResolver.GetSemanticId(specificAssetId);

                var specNode = valueNode.Children
                                        .OfType<SemanticLeafNode>()
                                        .FirstOrDefault(c => c.SemanticId == specSemanticId);

                if (specNode != null)
                {
                    specificAssetId.Value = specNode.Value;
                }
            }
        }
    }

    private static void FillOutProperty(Property valueElement, SemanticTreeNode values)
    {
        if (values is SemanticLeafNode leafValueNode)
        {
            valueElement.Value = leafValueNode.Value;
        }
    }

    private static void FillOutFile(File valueElement, SemanticTreeNode values)
    {
        if (values is SemanticLeafNode leafValueNode)
        {
            valueElement.Value = leafValueNode.Value;
        }
    }

    private static void FillOutBlob(Blob valueElement, SemanticTreeNode values)
    {
        if (values is SemanticLeafNode leafValueNode)
        {
            valueElement.Value = Convert.FromBase64String(leafValueNode.Value);
        }
    }

    private static void FillOutRange(Range valueElement, SemanticTreeNode values)
    {
        if (values is not SemanticBranchNode branchNode)
        {
            return;
        }

        var leafNodes = branchNode.Children.OfType<SemanticLeafNode>().ToList();

        valueElement.Min = leafNodes.FirstOrDefault(n => n.SemanticId
                                                          .EndsWith(SemanticIdResolver.RangeMinimumPostFixSeparator, StringComparison.Ordinal))?
                                                          .Value;

        valueElement.Max = leafNodes.FirstOrDefault(n => n.SemanticId
                                                          .EndsWith(SemanticIdResolver.RangeMaximumPostFixSeparator, StringComparison.Ordinal))?
                                                          .Value;
    }

    private void FillOutReferenceElement(ReferenceElement referenceElement, SemanticTreeNode semanticNode)
    {
        if (referenceElement?.Value?.Type != ReferenceTypes.ModelReference)
        {
            logger.LogInformation("ReferenceElement does not contain a ModelReference for SemanticId '{SemanticId}'. Skipping population.", semanticIdResolver.GetSemanticId(referenceElement!));
            return;
        }

        referenceHelper.PopulateReferenceKeys(referenceElement.Value, semanticNode, semanticIdResolver.GetSemanticId(referenceElement));
    }

    private void FillOutRelationshipElement(RelationshipElement relationshipElement, SemanticTreeNode semanticTreeNode)
    {
        var semanticId = semanticTreeNode.SemanticId;

        referenceHelper.PopulateRelationshipReference(relationshipElement.First, semanticTreeNode, semanticId, SemanticIdResolver.RelationshipElementFirstPostFixSeparator);

        referenceHelper.PopulateRelationshipReference(relationshipElement.Second, semanticTreeNode, semanticId, SemanticIdResolver.RelationshipElementSecondPostFixSeparator);
    }
}
