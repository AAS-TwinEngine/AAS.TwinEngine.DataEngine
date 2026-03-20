using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class RelationshipElementHandler(
    ISemanticIdResolver semanticIdResolver,
    IReferenceHelper referenceHelper) : ISubmodelElementTypeHandler
{
    public bool CanHandle(ISubmodelElement element) => element is RelationshipElement;

    public SemanticTreeNode? Extract(ISubmodelElement element, Func<ISubmodelElement, SemanticTreeNode?> extractChild)
    {
        var relationshipElement = (RelationshipElement)element;

        if (relationshipElement.First.Type == ReferenceTypes.ExternalReference && relationshipElement.Second.Type == ReferenceTypes.ExternalReference)
        {
            return null;
        }

        var semanticId = semanticIdResolver.ExtractSemanticId(relationshipElement);
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

    public void FillOut(ISubmodelElement element, SemanticTreeNode values, Action<List<ISubmodelElement>, SemanticTreeNode, bool> fillOutChildren)
    {
        var relationshipElement = (RelationshipElement)element;
        var semanticId = values.SemanticId;

        referenceHelper.PopulateRelationshipReference(relationshipElement.First, values, semanticId, SemanticIdResolver.RelationshipElementFirstPostFixSeparator);

        referenceHelper.PopulateRelationshipReference(relationshipElement.Second, values, semanticId, SemanticIdResolver.RelationshipElementSecondPostFixSeparator);
    }
}
