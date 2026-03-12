using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class ListHandler(
    ISemanticIdResolver semanticIdResolver,
    ILogger<ListHandler> logger) : ISubmodelElementTypeHandler
{
    public bool CanHandle(ISubmodelElement element) => element is SubmodelElementList;

    public SemanticTreeNode? Extract(ISubmodelElement element, Func<ISubmodelElement, SemanticTreeNode?> extractChild)
    {
        var list = (SubmodelElementList)element;
        var node = new SemanticBranchNode(semanticIdResolver.ResolveElementSemanticId(list, list.IdShort!), semanticIdResolver.GetCardinality(list));

        if (list.Value?.Count > 0)
        {
            foreach (var childNode in list.Value.Select(extractChild).OfType<SemanticTreeNode>())
            {
                node.AddChild(childNode);
            }
        }
        else
        {
            logger.LogWarning("No elements defined in SubmodelElementList {ListIdShort}", list.IdShort);
        }

        return node;
    }

    public void FillOut(ISubmodelElement element, SemanticTreeNode values, Action<List<ISubmodelElement>, SemanticTreeNode, bool> fillOutChildren)
    {
        var list = (SubmodelElementList)element;

        if (list?.Value == null || list.Value.Count == 0)
        {
            return;
        }

        fillOutChildren(list.Value, values, false);
    }
}
