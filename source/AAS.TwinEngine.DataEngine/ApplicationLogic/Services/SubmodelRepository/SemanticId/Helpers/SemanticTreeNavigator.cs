using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public static class SemanticTreeNavigator
{
    public static IEnumerable<SemanticTreeNode> FindBranchNodesBySemanticId(SemanticTreeNode tree, string semanticId)
    {
        var node = tree as SemanticBranchNode;

        return node?.Children!.Where(child => child.SemanticId.Equals(semanticId, StringComparison.Ordinal)) ?? [];
    }

    public static IEnumerable<SemanticTreeNode> FindNodeBySemanticId(SemanticTreeNode tree, string semanticId)
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

    public static IEnumerable<T> FindNodeBySemanticId<T>(SemanticTreeNode tree, string semanticId) where T : SemanticTreeNode
        => FindNodeBySemanticId(tree, semanticId).OfType<T>();
}
