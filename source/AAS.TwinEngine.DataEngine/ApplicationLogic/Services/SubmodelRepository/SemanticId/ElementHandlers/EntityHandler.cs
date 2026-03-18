using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class EntityHandler(
    ISemanticIdResolver semanticIdResolver,
    ILogger<EntityHandler> logger) : ISubmodelElementTypeHandler
{
    public bool CanHandle(ISubmodelElement element) => element is Entity;

    public SemanticTreeNode? Extract(ISubmodelElement element, Func<ISubmodelElement, SemanticTreeNode?> extractChild)
    {
        var entity = (Entity)element;
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
            foreach (var child in entity.Statements.Select(extractChild).OfType<SemanticTreeNode>())
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

    public void FillOut(ISubmodelElement element, SemanticTreeNode values, Action<List<ISubmodelElement>, SemanticTreeNode, bool> fillOutChildren)
    {
        var entity = (Entity)element;

        if (entity.EntityType == EntityType.SelfManagedEntity)
        {
            FillOutSelfManagedEntity(entity, values);
        }

        if (entity.Statements == null || entity.Statements.Count == 0)
        {
            return;
        }

        fillOutChildren(entity.Statements, values, true);
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
}
