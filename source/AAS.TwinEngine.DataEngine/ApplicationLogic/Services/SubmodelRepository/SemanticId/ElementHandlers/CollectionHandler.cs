using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class CollectionHandler(
    ISemanticIdResolver semanticIdResolver,
    ILogger<CollectionHandler> logger) : ISubmodelElementTypeHandler
{
    public bool CanHandle(ISubmodelElement element) => element is SubmodelElementCollection;

    public SemanticTreeNode? Extract(ISubmodelElement element, Func<ISubmodelElement, SemanticTreeNode?> extractChild)
    {
        var collection = (SubmodelElementCollection)element;
        var node = new SemanticBranchNode(semanticIdResolver.ResolveElementSemanticId(collection, collection.IdShort!), semanticIdResolver.GetCardinality(collection));

        if (collection.Value?.Count > 0)
        {
            foreach (var child in collection.Value.Where(_ => true))
            {
                var childNode = extractChild(child);
                if (childNode != null)
                {
                    node.AddChild(childNode);
                }
            }
        }
        else
        {
            logger.LogWarning("No elements defined in SubmodelElementCollection {CollectionIdShort}", collection.IdShort);
        }

        return node;
    }

    public void FillOut(ISubmodelElement element, SemanticTreeNode values, Action<List<ISubmodelElement>, SemanticTreeNode, bool> fillOutChildren)
    {
        var collection = (SubmodelElementCollection)element;

        if (collection?.Value == null || collection.Value.Count == 0)
        {
            return;
        }

        fillOutChildren(collection.Value, values, true);
    }
}
