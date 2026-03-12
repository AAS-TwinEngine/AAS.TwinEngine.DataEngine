using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class BlobHandler(ISemanticIdResolver semanticIdResolver) : ISubmodelElementTypeHandler
{
    public bool CanHandle(ISubmodelElement element) => element is Blob;

    public SemanticTreeNode? Extract(ISubmodelElement element, Func<ISubmodelElement, SemanticTreeNode?> extractChild)
    {
        var semanticId = semanticIdResolver.ResolveElementSemanticId(element, element.IdShort!);
        return new SemanticLeafNode(semanticId, string.Empty, semanticIdResolver.GetValueType(element), semanticIdResolver.GetCardinality(element));
    }

    public void FillOut(ISubmodelElement element, SemanticTreeNode values, Action<List<ISubmodelElement>, SemanticTreeNode, bool> fillOutChildren)
    {
        if (values is SemanticLeafNode leafValueNode)
        {
            ((Blob)element).Value = Convert.FromBase64String(leafValueNode.Value);
        }
    }
}
