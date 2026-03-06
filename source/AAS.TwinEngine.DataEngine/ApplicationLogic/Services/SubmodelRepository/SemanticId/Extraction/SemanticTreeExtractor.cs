using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Range = AasCore.Aas3_0.Range;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;

public class SemanticTreeExtractor(
    ISemanticIdResolver semanticIdResolver,
    ISubmodelElementHelper elementHelper,
    IReferenceHelper referenceHelper,
    ILogger<SemanticTreeExtractor> logger) : ISemanticTreeExtractor
{
    public SemanticTreeNode Extract(ISubmodel submodelTemplate)
    {
        ArgumentNullException.ThrowIfNull(submodelTemplate);

        var rootNode = new SemanticBranchNode(semanticIdResolver.ResolveSemanticId(submodelTemplate, submodelTemplate.IdShort!), Cardinality.Unknown);
        var childNodes = submodelTemplate.SubmodelElements!
                                         .Select(ExtractElement)
                                         .Where(childNode => childNode != null)
                                         .ToList();

        foreach (var childNode in childNodes)
        {
            rootNode.AddChild(childNode!);
        }

        return rootNode;
    }

    public ISubmodelElement Extract(ISubmodel submodelTemplate, string idShortPath)
    {
        ArgumentNullException.ThrowIfNull(submodelTemplate);
        ArgumentNullException.ThrowIfNull(idShortPath);

        var currentSubmodelElements = submodelTemplate.SubmodelElements;
        var idShortPathSegments = idShortPath.Split('.');
        for (var index = 0; index < idShortPathSegments.Length; index++)
        {
            var currentIdShort = idShortPathSegments[index];
            var isLastSegment = index == idShortPathSegments.Length - 1;

            var matchedElement = elementHelper.GetElementByIdShort(currentSubmodelElements, currentIdShort)
                                 ?? throw new InternalDataProcessingException();
            if (isLastSegment)
            {
                return matchedElement;
            }

            currentSubmodelElements = elementHelper.GetChildElements(matchedElement) as List<ISubmodelElement>
                                      ?? throw new InternalDataProcessingException();
        }

        throw new InternalDataProcessingException();
    }

    private SemanticTreeNode? ExtractElement(ISubmodelElement submodelElementTemplate)
    {
        ArgumentNullException.ThrowIfNull(submodelElementTemplate);

        return submodelElementTemplate switch
        {
            SubmodelElementCollection collection => ExtractCollection(collection),
            SubmodelElementList list => ExtractList(list),
            MultiLanguageProperty mlp => ExtractMultiLanguageProperty(mlp),
            Range range => ExtractRange(range),
            ReferenceElement re => ExtractReferenceElement(re),
            RelationshipElement relationshipElement => ExtractRelationshipElement(relationshipElement),
            Entity entity => ExtractEntity(entity),
            _ => CreateLeafNode(submodelElementTemplate)
        };
    }

    private SemanticBranchNode ExtractList(SubmodelElementList list)
    {
        var node = new SemanticBranchNode(semanticIdResolver.ResolveElementSemanticId(list, list.IdShort!), semanticIdResolver.GetCardinality(list));
        if (list.Value?.Count > 0)
        {
            foreach (var element in list.Value)
            {
                var child = ExtractElement(element);
                if (child != null)
                {
                    node.AddChild(child);
                }
            }
        }
        else
        {
            logger.LogWarning("No elements defined in SubmodelElementList {ListIdShort}", list.IdShort);
        }

        return node;
    }

    private SemanticBranchNode ExtractCollection(SubmodelElementCollection collection)
    {
        var node = new SemanticBranchNode(semanticIdResolver.ResolveElementSemanticId(collection, collection.IdShort!), semanticIdResolver.GetCardinality(collection));
        if (collection.Value?.Count > 0)
        {
            foreach (var element in collection.Value.Where(_ => true))
            {
                var child = ExtractElement(element);
                if (child != null)
                {
                    node.AddChild(child);
                }
            }
        }
        else
        {
            logger.LogWarning("No elements defined in SubmodelElementCollection {CollectionIdShort}", collection.IdShort);
        }

        return node;
    }

    private SemanticBranchNode? ExtractReferenceElement(ReferenceElement referenceElement)
    {
        if (referenceElement.Value == null || referenceElement.Value.Type == ReferenceTypes.ExternalReference)
        {
            return null;
        }

        return referenceHelper.ExtractReferenceKeys(referenceElement.Value, semanticIdResolver.ResolveElementSemanticId(referenceElement, referenceElement.IdShort!), semanticIdResolver.GetCardinality(referenceElement));
    }

    private SemanticBranchNode? ExtractRelationshipElement(RelationshipElement relationshipElement)
    {
        if (relationshipElement.First.Type == ReferenceTypes.ExternalReference && relationshipElement.Second.Type == ReferenceTypes.ExternalReference)
        {
            return null;
        }

        var semanticId = semanticIdResolver.GetSemanticId(relationshipElement);
        var cardinality = semanticIdResolver.GetCardinality(relationshipElement);
        var relationshipElementNode = new SemanticBranchNode(semanticId, cardinality);

        if (relationshipElement.First.Type == ReferenceTypes.ModelReference)
        {
            var referenceNode = referenceHelper.ExtractReferenceKeys(relationshipElement.First, $"{semanticId}{SemanticIdResolver.RelationshipElementFirstPostFixSeparator}", cardinality);
            if (referenceNode != null)
            {
                relationshipElementNode.AddChild(referenceNode);
            }
        }

        if (relationshipElement.Second.Type == ReferenceTypes.ModelReference)
        {
            var referenceNode = referenceHelper.ExtractReferenceKeys(relationshipElement.Second, $"{semanticId}{SemanticIdResolver.RelationshipElementSecondPostFixSeparator}", cardinality);
            if (referenceNode != null)
            {
                relationshipElementNode.AddChild(referenceNode);
            }
        }

        return relationshipElementNode;
    }

    private SemanticBranchNode ExtractEntity(Entity entity)
    {
        var semanticId = semanticIdResolver.ResolveElementSemanticId(entity, entity.IdShort!);
        var node = new SemanticBranchNode(semanticId, semanticIdResolver.GetCardinality(entity));
        if (entity.EntityType == EntityType.SelfManagedEntity)
        {
            var globalAssetIdNode = new SemanticLeafNode(semanticId + SemanticIdResolver.EntityGlobalAssetIdPostFix, string.Empty, DataType.String, Cardinality.One);
            node.AddChild(globalAssetIdNode);
            if (entity.SpecificAssetIds != null)
            {
                foreach (var specificAssetId in entity.SpecificAssetIds)
                {
                    IHasSemantics specificAsset = specificAssetId;
                    if (specificAsset.SemanticId == null)
                    {
                        continue;
                    }

                    var specificAssetIdNode = new SemanticLeafNode(semanticIdResolver.GetSemanticId(specificAssetId), string.Empty, DataType.String, Cardinality.One);
                    node.AddChild(specificAssetIdNode);
                }
            }
        }

        if (entity.Statements?.Count > 0)
        {
            foreach (var child in entity.Statements.Select(ExtractElement).OfType<SemanticTreeNode>())
            {
                node.AddChild(child);
            }
        }
        else
        {
            logger.LogWarning("No elements defined in Entity {EntityIdShort}", entity.IdShort);
        }

        return node;
    }

    private SemanticBranchNode? ExtractMultiLanguageProperty(MultiLanguageProperty mlp)
    {
        var semanticId = semanticIdResolver.ExtractSemanticId(mlp);
        var node = new SemanticBranchNode(semanticId, semanticIdResolver.GetCardinality(mlp));

        var languages = elementHelper.ResolveLanguages(mlp);

        if (mlp.Value is not { Count: > 0 })
        {
            logger.LogInformation("No languages defined in template for MultiLanguageProperty {MlpIdShort}", mlp.IdShort);
        }

        var mlpSeparator = semanticIdResolver.MlpPostFixSeparator;
        foreach (var langSemanticId in languages.Select(language => string.Concat(semanticId, mlpSeparator, language)))
        {
            node.AddChild(new SemanticLeafNode(langSemanticId, string.Empty, DataType.String, Cardinality.ZeroToOne));
        }

        return node;
    }

    private SemanticBranchNode ExtractRange(Range range)
    {
        var semanticId = semanticIdResolver.ExtractSemanticId(range);
        var valueType = semanticIdResolver.GetValueType(range);
        var node = new SemanticBranchNode(semanticId, semanticIdResolver.GetCardinality(range));

        node.AddChild(new SemanticLeafNode(semanticId + SemanticIdResolver.RangeMinimumPostFixSeparator, string.Empty, valueType, Cardinality.ZeroToOne));
        node.AddChild(new SemanticLeafNode(semanticId + SemanticIdResolver.RangeMaximumPostFixSeparator, string.Empty, valueType, Cardinality.ZeroToOne));

        return node;
    }

    private SemanticLeafNode CreateLeafNode(ISubmodelElement element)
    {
        var semanticId = semanticIdResolver.ResolveElementSemanticId(element, element.IdShort!);
        var valueType = semanticIdResolver.GetValueType(element);
        var cardinality = semanticIdResolver.GetCardinality(element);
        return new SemanticLeafNode(semanticId, string.Empty, valueType, cardinality);
    }
}
