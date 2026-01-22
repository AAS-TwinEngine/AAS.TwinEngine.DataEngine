using System.Globalization;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Options;

using File = AasCore.Aas3_0.File;
using Range = AasCore.Aas3_0.Range;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public partial class SemanticIdHandler(ILogger<SemanticIdHandler> logger, IOptions<Semantics> semantics) : ISemanticIdHandler
{
    private readonly string _mlpPostFixSeparator = semantics.Value.MultiLanguageSemanticPostfixSeparator;
    private readonly string _submodelElementIndexContextPrefix = semantics.Value.SubmodelElementIndexContextPrefix;
    private readonly string _internalSemanticId = semantics.Value.InternalSemanticId;
    private const string RangeMinimumPostFixSeparator = "_min";
    private const string RangeMaximumPostFixSeparator = "_max";
    private const string EntityGlobalAssetIdPostFix = "_globalAssetId";
    private const string RelationshipElementFirstPostFixSeparator = "_first";
    private const string RelationshipElementSecondPostFixSeparator = "_second";

    private static readonly HashSet<DataTypeDefXsd> StringTypes =
    [
        DataTypeDefXsd.String, DataTypeDefXsd.AnyUri, DataTypeDefXsd.Byte, DataTypeDefXsd.Date,
        DataTypeDefXsd.DateTime, DataTypeDefXsd.Duration, DataTypeDefXsd.GDay, DataTypeDefXsd.GYear,
        DataTypeDefXsd.GYearMonth, DataTypeDefXsd.HexBinary, DataTypeDefXsd.Time, DataTypeDefXsd.Base64Binary,
        DataTypeDefXsd.GMonth, DataTypeDefXsd.GMonthDay
    ];

    private static readonly HashSet<DataTypeDefXsd> IntegerTypes =
    [
        DataTypeDefXsd.Int, DataTypeDefXsd.Integer, DataTypeDefXsd.Long, DataTypeDefXsd.NegativeInteger,
        DataTypeDefXsd.NonNegativeInteger, DataTypeDefXsd.NonPositiveInteger, DataTypeDefXsd.PositiveInteger,
        DataTypeDefXsd.Short, DataTypeDefXsd.UnsignedShort, DataTypeDefXsd.UnsignedLong,
        DataTypeDefXsd.UnsignedInt, DataTypeDefXsd.UnsignedByte
    ];

    private static readonly HashSet<DataTypeDefXsd> NumberTypes =
    [
        DataTypeDefXsd.Float, DataTypeDefXsd.Double, DataTypeDefXsd.Decimal
    ];

    public SemanticTreeNode Extract(ISubmodel submodelTemplate)
    {
        ArgumentNullException.ThrowIfNull(submodelTemplate);

        var rootNode = new SemanticBranchNode(GetSemanticId(submodelTemplate, submodelTemplate.IdShort!), Cardinality.Unknown);
        var childNodes = submodelTemplate.SubmodelElements!
                                         .Select(Extract)
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

            var matchedElement = GetElementByIdShort(currentSubmodelElements, currentIdShort)
                                 ?? throw new InternalDataProcessingException();
            if (isLastSegment)
            {
                return matchedElement;
            }

            currentSubmodelElements = GetChildElements(matchedElement) as List<ISubmodelElement>
                                      ?? throw new InternalDataProcessingException();
        }

        throw new InternalDataProcessingException();
    }

    private SemanticTreeNode? Extract(ISubmodelElement submodelElementTemplate)
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
        var node = new SemanticBranchNode(GetSemanticId(list, list.IdShort!), GetCardinality(list));
        if (list.Value?.Count > 0)
        {
            foreach (var element in list.Value)
            {
                var child = Extract(element);
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
        var node = new SemanticBranchNode(GetSemanticId(collection, collection.IdShort!), GetCardinality(collection));
        if (collection.Value?.Count > 0)
        {
            foreach (var element in collection.Value.Where(_ => true))
            {
                var child = Extract(element);
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

        return ExtractFormReference(referenceElement.Value, GetSemanticId(referenceElement, referenceElement.IdShort!), GetCardinality(referenceElement));
    }

    private SemanticBranchNode? ExtractRelationshipElement(RelationshipElement relationshipElement)
    {
        if (relationshipElement.First.Type == ReferenceTypes.ExternalReference && relationshipElement.Second.Type == ReferenceTypes.ExternalReference)
        {
            return null;
        }

        var semanticId = GetSemanticId(relationshipElement);
        var cardinality = GetCardinality(relationshipElement);
        var relationshipElementNode = new SemanticBranchNode(semanticId, cardinality);

        if (relationshipElement.First.Type == ReferenceTypes.ModelReference)
        {
            var referenceNode = ExtractFormReference(relationshipElement.First, $"{semanticId}{RelationshipElementFirstPostFixSeparator}", cardinality);
            if (referenceNode != null)
            {
                relationshipElementNode.AddChild(referenceNode);
            }
        }

        if (relationshipElement.Second.Type == ReferenceTypes.ModelReference)
        {
            var referenceNode = ExtractFormReference(relationshipElement.Second, $"{semanticId}{RelationshipElementSecondPostFixSeparator}", cardinality);
            if (referenceNode != null)
            {
                relationshipElementNode.AddChild(referenceNode);
            }
        }

        return relationshipElementNode;
    }

    private SemanticBranchNode? ExtractFormReference(IReference reference, string semanticId, Cardinality cardinality)
    {
        var keys = reference.Keys;
        if (keys.Count <= 0)
        {
            return null;
        }

        var branchNode = new SemanticBranchNode(semanticId, cardinality);

        foreach (var group in keys.GroupBy(k => k.Type))
        {
            group.Select((_, index) => new SemanticLeafNode(group.Count() > 1
                                                                  ? $"{semanticId}{_mlpPostFixSeparator}{group.Key}{_mlpPostFixSeparator}{index}"
                                                                  : $"{semanticId}{_mlpPostFixSeparator}{group.Key}",
                                                              string.Empty,
                                                              DataType.String,
                                                              Cardinality.ZeroToOne))
                 .ToList()
                 .ForEach(branchNode.AddChild);
        }

        return branchNode;
    }

    private SemanticBranchNode ExtractEntity(Entity entity)
    {
        var semanticId = GetSemanticId(entity, entity.IdShort!);
        var node = new SemanticBranchNode(semanticId, GetCardinality(entity));
        if (entity.EntityType == EntityType.SelfManagedEntity)
        {
            var globalAssetIdNode = new SemanticLeafNode(semanticId + EntityGlobalAssetIdPostFix, string.Empty, DataType.String, Cardinality.One);
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

                    var specificAssetIdNode = new SemanticLeafNode(GetSemanticId(specificAssetId), string.Empty, DataType.String, Cardinality.One);
                    node.AddChild(specificAssetIdNode);
                }
            }
        }

        if (entity.Statements?.Count > 0)
        {
            foreach (var child in entity.Statements.Select(Extract).OfType<SemanticTreeNode>())
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

    private string ExtractSemanticId(ISubmodelElement element)
    {
        if (element.Qualifiers == null)
        {
            return GetSemanticId(element);
        }

        var qualifier = element.Qualifiers.FirstOrDefault(q => q.Type == _internalSemanticId);
        if (qualifier != null)
        {
            return qualifier.Value!;
        }

        return GetSemanticId(element);
    }

    private SemanticBranchNode? ExtractMultiLanguageProperty(MultiLanguageProperty mlp)
    {
        var semanticId = ExtractSemanticId(mlp);

        var node = new SemanticBranchNode(semanticId, GetCardinality(mlp));
        if (mlp.Value == null)
        {
            logger.LogWarning("No languages defined in MultiLanguageProperty {MlpIdShort}", mlp.IdShort);
            return node;
        }

        foreach (var langSemanticId in mlp.Value.Select(langValue => semanticId + _mlpPostFixSeparator + langValue.Language))
        {
            node.AddChild(new SemanticLeafNode(langSemanticId, string.Empty, DataType.String, Cardinality.ZeroToOne));
        }

        return node;
    }

    private SemanticBranchNode ExtractRange(Range range)
    {
        var semanticId = ExtractSemanticId(range);
        var valueType = GetValueType(range);
        var node = new SemanticBranchNode(semanticId, GetCardinality(range));

        node.AddChild(new SemanticLeafNode(semanticId + RangeMinimumPostFixSeparator, string.Empty, valueType, Cardinality.ZeroToOne));
        node.AddChild(new SemanticLeafNode(semanticId + RangeMaximumPostFixSeparator, string.Empty, valueType, Cardinality.ZeroToOne));

        return node;
    }

    private SemanticLeafNode CreateLeafNode(ISubmodelElement element)
    {
        var semanticId = GetSemanticId(element, element.IdShort!);
        var valueType = GetValueType(element);
        var cardinality = GetCardinality(element);
        return new SemanticLeafNode(semanticId, string.Empty, valueType, cardinality);
    }

    private static Cardinality GetCardinality(ISubmodelElement element)
    {
        var qualifierValue = element.Qualifiers?.FirstOrDefault()?.Value;
        if (qualifierValue is null)
        {
            return Cardinality.Unknown;
        }

        return Enum.TryParse<Cardinality>(qualifierValue, ignoreCase: true, out var result)
                   ? result
                   : Cardinality.Unknown;
    }

    private static DataType GetValueType(ISubmodelElement element)
    {
        return element switch
        {
            Property p => GetDataTypeFromValueType(p.ValueType),
            Range r => GetDataTypeFromValueType(r.ValueType),
            File => DataType.String,
            Blob => DataType.String,
            _ => DataType.Unknown
        };
    }

    private static DataType GetDataTypeFromValueType(DataTypeDefXsd valueType)
    {
        return valueType switch
        {
            _ when StringTypes.Contains(valueType) => DataType.String,
            _ when IntegerTypes.Contains(valueType) => DataType.Integer,
            _ when NumberTypes.Contains(valueType) => DataType.Number,
            DataTypeDefXsd.Boolean => DataType.Boolean,
            _ => DataType.Unknown
        };
    }

    private string GetSemanticId(ISubmodelElement element, string idShort)
    {
        var baseSemanticId = ExtractSemanticId(element);
        return AppendIndex(baseSemanticId, idShort);
    }

    private string GetSemanticId(IHasSemantics hasSemantics, string idShort)
    {
        var baseSemanticId = GetSemanticId(hasSemantics);
        return AppendIndex(baseSemanticId, idShort);
    }

    private static string GetSemanticId(IHasSemantics hasSemantics) => hasSemantics.SemanticId?.Keys?.FirstOrDefault()?.Value ?? string.Empty;

    private string AppendIndex(string semanticId, string? idShort)
    {
        var index = string.Empty;
        if (idShort != null)
        {
            index = SubmodelElementCollectionIndex().Match(idShort).Value;
        }

        return string.IsNullOrWhiteSpace(index)
                   ? semanticId
                   : $"{semanticId}{_submodelElementIndexContextPrefix}{index}";
    }

    public ISubmodel FillOutTemplate(ISubmodel submodelTemplate, SemanticTreeNode values)
    {
        ArgumentNullException.ThrowIfNull(submodelTemplate);
        ArgumentNullException.ThrowIfNull(submodelTemplate.SubmodelElements);
        ArgumentNullException.ThrowIfNull(values);

        var submodelElements = submodelTemplate.SubmodelElements.ToList();
        foreach (var submodelElement in submodelElements)
        {
            var semanticId = ExtractSemanticId(submodelElement);

            var matchingNodes = FindBranchNodesBySemanticId(values, semanticId)?.ToList();

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
            var clonedElement = CloneElementJson(baseElement);

            if (baseElement is SubmodelElementCollection)
            {
                clonedElement.IdShort = $"{clonedElement.IdShort}{i}";
            }

            _ = FillOutTemplate(clonedElement, node);
            submodelTemplate.SubmodelElements?.Add(clonedElement);
        }
    }

    private void HandleSingleMatchingNode(
        SemanticTreeNode node,
        ISubmodelElement element,
        ISubmodel submodelTemplate)
    {
        _ = FillOutTemplate(element, node);
        submodelTemplate.SubmodelElements?.Add(element);
    }

    private ISubmodelElement FillOutTemplate(ISubmodelElement submodelElementTemplate, SemanticTreeNode values)
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

    public void FillOutSubmodelElementList(SubmodelElementList list, SemanticTreeNode values)
    {
        if (list?.Value == null || list.Value.Count == 0)
        {
            return;
        }

        FillOutSubmodelElementValue(list.Value, values, false);
    }

    public void FillOutSubmodelElementCollection(SubmodelElementCollection collection, SemanticTreeNode values)
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
            var valueNode = FindNodeBySemanticId(values, ExtractSemanticId(element));
            var semanticTreeNodes = valueNode?.ToList();

            if (semanticTreeNodes == null || semanticTreeNodes.Count == 0)
            {
                continue;
            }

            if (!AreAllNodesOfSameType(semanticTreeNodes, out _))
            {
                logger.LogWarning("Mixed node types found for element '{IdShort}' with SemanticId '{SemanticId}'. Expected all nodes to be either SemanticBranchNode or SemanticLeafNode. Removing element.",
                                  element.IdShort,
                                  ExtractSemanticId(element));
                _ = elements.Remove(element);
                continue;
            }

            if (semanticTreeNodes.Count > 1 && element is not Property && element is not ReferenceElement)
            {
                _ = elements.Remove(element);
                for (var i = 0; i < semanticTreeNodes.Count; i++)
                {
                    var cloned = CloneElementJson(element);
                    if (updateIdShort)
                    {
                        cloned.IdShort = $"{cloned.IdShort}{i}";
                    }

                    _ = FillOutTemplate(cloned, semanticTreeNodes[i]);
                    elements.Add(cloned);
                }
            }
            else
            {
                HandleSingleSemanticTreeNode(element, semanticTreeNodes[0]);
            }
        }
    }

    private static bool AreAllNodesOfSameType(List<SemanticTreeNode> nodes, out Type? nodeType)
    {
        if (nodes.Count == 0)
        {
            nodeType = null;
            return true;
        }

        var firstNodeType = nodes[0].GetType();
        nodeType = firstNodeType;

        if (firstNodeType != typeof(SemanticBranchNode) && firstNodeType != typeof(SemanticLeafNode))
        {
            return false;
        }

        return nodes.All(node => node.GetType() == firstNodeType);
    }

    private void HandleSingleSemanticTreeNode(ISubmodelElement element, SemanticTreeNode node) => FillOutTemplate(element, node);

    private void FillOutMultiLanguageProperty(MultiLanguageProperty mlp, SemanticTreeNode values)
    {
        if (mlp.Value == null)
        {
            return;
        }

        var semanticId = ExtractSemanticId(mlp);
        var valueNode = FindNodeBySemanticId(values, semanticId).First() as SemanticBranchNode;

        foreach (var languageValue in mlp.Value)
        {
            var languageSemanticId = semanticId + _mlpPostFixSeparator + languageValue.Language;

            var leafNode = valueNode?.Children
                                    .OfType<SemanticLeafNode>()
                                    .FirstOrDefault(child => child.SemanticId == languageSemanticId);

            if (leafNode != null)
            {
                languageValue.Text = leafNode.Value;
            }
        }
    }

    public void FillOutEntity(Entity entity, SemanticTreeNode values)
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
        var semanticId = GetSemanticId(entity, entity.IdShort!);

        if (FindNodeBySemanticId(values, semanticId).FirstOrDefault() is not SemanticBranchNode valueNode)
        {
            return;
        }

        var globalAssetSemanticId = semanticId + EntityGlobalAssetIdPostFix;

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
                var specSemanticId = GetSemanticId(specificAssetId);

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
                                                          .EndsWith(RangeMinimumPostFixSeparator, StringComparison.Ordinal))?
                                                          .Value;

        valueElement.Max = leafNodes.FirstOrDefault(n => n.SemanticId
                                                          .EndsWith(RangeMaximumPostFixSeparator, StringComparison.Ordinal))?
                                                          .Value;
    }

    private void FillOutReferenceElement(ReferenceElement referenceElement, SemanticTreeNode semanticNode)
    {
        if (referenceElement?.Value?.Type != ReferenceTypes.ModelReference)
        {
            logger.LogInformation("ReferenceElement does not contain a ModelReference for SemanticId '{SemanticId}'. Skipping population.", GetSemanticId(referenceElement!));
            return;
        }

        ProcessReferenceNode(referenceElement.Value, semanticNode, GetSemanticId(referenceElement));
    }

    private void FillOutRelationshipElement(RelationshipElement relationshipElement, SemanticTreeNode semanticTreeNode)
    {
        var semanticId = semanticTreeNode.SemanticId;

        ProcessRelationshipReference(relationshipElement.First, semanticTreeNode, semanticId, RelationshipElementFirstPostFixSeparator);

        ProcessRelationshipReference(relationshipElement.Second, semanticTreeNode, semanticId, RelationshipElementSecondPostFixSeparator);
    }

    private void ProcessReferenceNode(IReference reference, SemanticTreeNode semanticNode, string semanticId)
    {
        if (semanticNode is not SemanticBranchNode branchNode)
        {
            logger.LogWarning("Expected SemanticBranchNode for SemanticId '{SemanticId}', but got {NodeType}. Skipping population.", semanticId, semanticNode.GetType().Name);
            return;
        }

        var keys = reference.Keys;

        if (keys.Count <= 0)
        {
            logger.LogInformation("ReferenceElement has no keys for SemanticId '{SemanticId}'. Nothing to populate.", semanticId);
            return;
        }

        foreach (var group in keys.GroupBy(k => k.Type))
        {
            ProcessReferenceKeyGroup(group, branchNode, semanticId);
        }
    }

    private void ProcessReferenceKeyGroup(IGrouping<KeyTypes, IKey> group, SemanticBranchNode branchNode, string semanticId)
    {
        var keyList = group.ToList();
        for (var i = 0; i < keyList.Count; i++)
        {
            var indexedSemanticId = keyList.Count > 1
                                        ? $"{semanticId}{_mlpPostFixSeparator}{group.Key}{_mlpPostFixSeparator}{i}"
                                        : $"{semanticId}{_mlpPostFixSeparator}{group.Key}";

            var leafNode = branchNode.Children
                                     .OfType<SemanticLeafNode>()
                                     .FirstOrDefault(child => child.SemanticId == indexedSemanticId);

            if (leafNode != null)
            {
                keyList[i].Value = !string.IsNullOrEmpty(leafNode.Value) ? leafNode.Value : keyList[i].Value;
            }
            else
            {
                logger.LogWarning("No matching leaf node found for SemanticId '{IndexedSemanticId}'.", indexedSemanticId);
            }
        }
    }

    private void ProcessRelationshipReference(IReference reference, SemanticTreeNode semanticTreeNode, string semanticId, string postfixSeparator)
    {
        if (reference.Type != ReferenceTypes.ModelReference)
        {
            return;
        }

        var searchPattern = semanticId + postfixSeparator;
        var valueNode = FindNodeBySemanticId(semanticTreeNode, searchPattern).FirstOrDefault();

        if (valueNode != null)
        {
            ProcessReferenceNode(reference, valueNode, searchPattern);
        }
        else
        {
            logger.LogWarning("No matching node found for reference with pattern: {Pattern}", searchPattern);
        }
    }

    private static ISubmodelElement CloneElementJson(ISubmodelElement element)
    {
        var jsonElement = Jsonization.Serialize.ToJsonObject(element);

        return Jsonization.Deserialize.ISubmodelElementFrom(jsonElement);
    }

    private static IEnumerable<SemanticTreeNode> FindBranchNodesBySemanticId(SemanticTreeNode tree, string semanticId)
    {
        var node = tree as SemanticBranchNode;

        return node?.Children!
                   .Where(child => child.SemanticId.Equals(semanticId, StringComparison.Ordinal))
               ?? [];
    }

    private static IEnumerable<SemanticTreeNode> FindNodeBySemanticId(SemanticTreeNode tree, string semanticId)
    {
        if (tree.SemanticId == semanticId)
        {
            yield return tree;
        }

        if (tree is not SemanticBranchNode branchNode)
        {
            yield break;
        }

        foreach (var child in branchNode.Children)
        {
            foreach (var matchingNode in FindNodeBySemanticId(child, semanticId))
            {
                yield return matchingNode;
            }
        }
    }

    /// <summary>
    /// Matches strings like "element[3]" and captures:
    ///   Group 1 → element name (any characters, lazy match)
    ///   Group 2 → index (digits inside square brackets)
    /// e.g. "element[3]" -> matches Group1= "element", Group2 = "3"
    /// Pattern: ^(.+?)\[(\d+)\]$
    /// </summary>
    [GeneratedRegex(@"^(.+?)(?:\[(\d+)\]|%5B(\d+)%5D)$")]
    private static partial Regex SubmodelElementListIndex();

    /// <summary>
    /// Matches one or more digits at the end of a string,
    /// e.g., "element42" → matches "42"
    /// Pattern: \d+$
    /// </summary>
    [GeneratedRegex(@"\d+$")]
    private static partial Regex SubmodelElementCollectionIndex();

    private ISubmodelElement? GetElementByIdShort(IEnumerable<ISubmodelElement>? submodelElements, string idShort)
    {
        if (TryParseIdShortWithBracketIndex(idShort, out var idShortWithoutIndex, out var index))
        {
            return GetElementFromListByIndex(submodelElements, idShortWithoutIndex, index);
        }

        return submodelElements?.FirstOrDefault(e => e.IdShort == idShort);
    }

    private static bool TryParseIdShortWithBracketIndex(string idShort, out string idShortWithoutIndex, out int index)
    {
        var match = SubmodelElementListIndex().Match(idShort);
        if (!match.Success)
        {
            idShortWithoutIndex = string.Empty;
            index = -1;
            return false;
        }

        idShortWithoutIndex = match.Groups[1].Value;
        var indexGroup = match.Groups[2].Success ? match.Groups[2] : match.Groups[3];
        if (!indexGroup.Success)
        {
            idShortWithoutIndex = string.Empty;
            index = -1;
            return false;
        }

        index = int.Parse(indexGroup.Value, CultureInfo.InvariantCulture);
        return true;
    }

    private ISubmodelElement GetElementFromListByIndex(IEnumerable<ISubmodelElement>? elements, string idShortWithoutIndex, int index)
    {
        var baseElement = elements?.FirstOrDefault(e => e.IdShort == idShortWithoutIndex);

        if (baseElement is not ISubmodelElementList list)
        {
            logger.LogError("Expected list element with IdShort '{IdShortWithoutIndex}' not found or is not a list.", idShortWithoutIndex);
            throw new InternalDataProcessingException();
        }

        if (index >= 0 && index < list.Value!.Count)
        {
            return list.Value[index];
        }

        logger.LogError("Index {Index} is out of bounds for list '{IdShortWithoutIndex}' with count {Count}.", index, idShortWithoutIndex, list.Value!.Count);
        throw new InternalDataProcessingException();
    }

    private static List<ISubmodelElement>? GetChildElements(ISubmodelElement submodelElement)
    {
        return submodelElement switch
        {
            ISubmodelElementCollection c => c.Value,
            ISubmodelElementList l => l.Value,
            IEntity entity => entity.Statements,
            _ => null
        };
    }
}
