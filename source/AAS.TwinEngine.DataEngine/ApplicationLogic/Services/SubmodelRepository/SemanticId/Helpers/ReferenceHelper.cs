using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public class ReferenceHelper(
    ISemanticIdResolver semanticIdResolver,
    ILogger<ReferenceHelper> logger) : IReferenceHelper
{
    public SemanticBranchNode? ExtractReferenceKeys(IReference reference, string semanticId, Cardinality cardinality)
    {
        var keys = reference.Keys;
        if (keys.Count <= 0)
        {
            return null;
        }

        var branchNode = new SemanticBranchNode(semanticId, cardinality);

        foreach (var group in keys.GroupBy(k => k.Type))
        {
            group.Select((_, index) => new SemanticLeafNode(
                                           semanticIdResolver.BuildReferenceKeySemanticId(semanticId, group.Key, index, group.Count()),
                                           string.Empty,
                                           DataType.String,
                                           Cardinality.ZeroToOne))
                 .ToList()
                 .ForEach(branchNode.AddChild);
        }

        return branchNode;
    }

    public void PopulateReferenceKeys(IReference reference, SemanticTreeNode semanticNode, string semanticId)
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
            PopulateReferenceKeyGroup(group, branchNode, semanticId);
        }
    }

    public void PopulateRelationshipReference(IReference reference, SemanticTreeNode semanticTreeNode, string semanticId, string postfixSeparator)
    {
        if (reference.Type != ReferenceTypes.ModelReference)
        {
            return;
        }

        var searchPattern = semanticId + postfixSeparator;
        var valueNode = SemanticTreeNavigator.FindNodeBySemanticId(semanticTreeNode, searchPattern).FirstOrDefault();

        if (valueNode != null)
        {
            PopulateReferenceKeys(reference, valueNode, searchPattern);
        }
        else
        {
            logger.LogWarning("No matching node found for reference with pattern: {Pattern}", searchPattern);
        }
    }

    private void PopulateReferenceKeyGroup(IGrouping<KeyTypes, IKey> group, SemanticBranchNode branchNode, string semanticId)
    {
        var keyList = group.ToList();
        for (var i = 0; i < keyList.Count; i++)
        {
            var indexedSemanticId = semanticIdResolver.BuildReferenceKeySemanticId(semanticId, group.Key, i, keyList.Count);

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
}
