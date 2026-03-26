using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public class SemanticTreeNavigatorTests
{
    [Fact]
    public void FindBranchNodesBySemanticId_ReturnsMatchingChildren()
    {
        var root = new SemanticBranchNode("root", Cardinality.Unknown);
        var child1 = new SemanticBranchNode("target", Cardinality.One);
        var child2 = new SemanticBranchNode("target", Cardinality.ZeroToOne);
        var child3 = new SemanticBranchNode("other", Cardinality.One);
        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        var result = SemanticTreeNavigator.FindBranchNodesBySemanticId(root, "target").ToList();

        Equal(2, result.Count);
        Contains(child1, result);
        Contains(child2, result);
    }

    [Fact]
    public void FindBranchNodesBySemanticId_NoMatch_ReturnsEmpty()
    {
        var root = new SemanticBranchNode("root", Cardinality.Unknown);
        root.AddChild(new SemanticBranchNode("other", Cardinality.One));

        var result = SemanticTreeNavigator.FindBranchNodesBySemanticId(root, "nonexistent").ToList();

        Empty(result);
    }

    [Fact]
    public void FindBranchNodesBySemanticId_LeafNode_ReturnsEmpty()
    {
        var leaf = new SemanticLeafNode("leaf", "value", DataType.String, Cardinality.One);

        var result = SemanticTreeNavigator.FindBranchNodesBySemanticId(leaf, "leaf").ToList();

        Empty(result);
    }

    [Fact]
    public void FindNodeBySemanticId_ReturnsMatchingNode_AtRoot()
    {
        var root = new SemanticBranchNode("target", Cardinality.Unknown);

        var result = SemanticTreeNavigator.FindNodeBySemanticId(root, "target").ToList();

        Single(result);
        Same(root, result[0]);
    }

    [Fact]
    public void FindNodeBySemanticId_ReturnsMatchingNodes_InNestedTree()
    {
        var root = new SemanticBranchNode("root", Cardinality.Unknown);
        var child = new SemanticBranchNode("branch", Cardinality.One);
        var grandchild = new SemanticLeafNode("target", "val", DataType.String, Cardinality.One);
        child.AddChild(grandchild);
        root.AddChild(child);

        var result = SemanticTreeNavigator.FindNodeBySemanticId(root, "target").ToList();

        Single(result);
        Same(grandchild, result[0]);
    }

    [Fact]
    public void FindNodeBySemanticId_ReturnsMultipleMatches_AcrossTree()
    {
        var root = new SemanticBranchNode("root", Cardinality.Unknown);
        var leaf1 = new SemanticLeafNode("target", "v1", DataType.String, Cardinality.One);
        var branch = new SemanticBranchNode("branch", Cardinality.One);
        var leaf2 = new SemanticLeafNode("target", "v2", DataType.String, Cardinality.One);
        branch.AddChild(leaf2);
        root.AddChild(leaf1);
        root.AddChild(branch);

        var result = SemanticTreeNavigator.FindNodeBySemanticId(root, "target").ToList();

        Equal(2, result.Count);
    }

    [Fact]
    public void FindNodeBySemanticId_NoMatch_ReturnsEmpty()
    {
        var root = new SemanticBranchNode("root", Cardinality.Unknown);
        root.AddChild(new SemanticLeafNode("other", "val", DataType.String, Cardinality.One));

        var result = SemanticTreeNavigator.FindNodeBySemanticId(root, "nonexistent").ToList();

        Empty(result);
    }

    [Fact]
    public void FindNodeBySemanticId_Generic_WhenSameSemanticIdOnBothBranchAndLeaf_ReturnsOnlyRequestedType()
    {
        var root = new SemanticBranchNode("root", Cardinality.Unknown);
        var branch = new SemanticBranchNode("shared", Cardinality.One);
        var leaf = new SemanticLeafNode("shared", "val", DataType.String, Cardinality.One);
        root.AddChild(branch);
        root.AddChild(leaf);

        var branchResults = SemanticTreeNavigator.FindNodeBySemanticId<SemanticBranchNode>(root, "shared").ToList();
        var leafResults = SemanticTreeNavigator.FindNodeBySemanticId<SemanticLeafNode>(root, "shared").ToList();

        Single(branchResults);
        Same(branch, branchResults[0]);
        Single(leafResults);
        Same(leaf, leafResults[0]);
    }
}
