using System.Collections.ObjectModel;

namespace AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

public abstract class SemanticTreeNode(string semanticId, DataType dataType)
{
    public string SemanticId { get; set; } = semanticId;

    public DataType DataType { get; set; } = dataType;
}

public class SemanticBranchNode(string semanticId, DataType dataType) : SemanticTreeNode(semanticId, dataType)
{
    private readonly List<SemanticTreeNode> _children = [];

    public ReadOnlyCollection<SemanticTreeNode> Children => _children.AsReadOnly();

    public void AddChild(SemanticTreeNode child) => _children.Add(child);

    public void ReplaceChildren(List<SemanticTreeNode> newChildren)
    {
        _children.Clear();
        _children.AddRange(newChildren);
    }
}

public class SemanticLeafNode(string semanticId, DataType dataType, string value) : SemanticTreeNode(semanticId, dataType)
{
    public string Value { get; set; } = value;
}

