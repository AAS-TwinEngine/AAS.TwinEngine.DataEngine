using System.Collections.ObjectModel;

namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

public abstract class SemanticTreeNode(string semanticId, Cardinality cardinality)
{
    public string SemanticId { get; set; } = semanticId;

    public Cardinality Cardinality { get; set; } = cardinality;
}

public class SemanticBranchNode(string semanticId, Cardinality cardinality) : SemanticTreeNode(semanticId, cardinality)
{
    private readonly List<SemanticTreeNode> _children = [];

    public ReadOnlyCollection<SemanticTreeNode> Children => _children.AsReadOnly();

    public void AddChild(SemanticTreeNode child) => _children.Add(child);
}

public class SemanticLeafNode(string semanticId, dynamic value, DataType dataType, Cardinality cardinality) : SemanticTreeNode(semanticId, cardinality)
{
    public dynamic Value { get; set; } = value;

    public DataType DataType { get; set; } = dataType;
}
