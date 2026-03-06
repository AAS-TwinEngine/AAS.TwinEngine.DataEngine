using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;

public interface IReferenceHelper
{
    SemanticBranchNode? ExtractReferenceKeys(IReference reference, string semanticId, Cardinality cardinality);

    void PopulateReferenceKeys(IReference reference, SemanticTreeNode semanticNode, string semanticId);

    void PopulateRelationshipReference(IReference reference, SemanticTreeNode semanticTreeNode, string semanticId, string postfixSeparator);
}
