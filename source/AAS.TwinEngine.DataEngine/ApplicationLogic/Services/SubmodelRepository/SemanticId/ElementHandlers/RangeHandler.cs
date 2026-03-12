using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Range = AasCore.Aas3_0.Range;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class RangeHandler(ISemanticIdResolver semanticIdResolver) : ISubmodelElementTypeHandler
{
    public bool CanHandle(ISubmodelElement element) => element is Range;

    public SemanticTreeNode? Extract(ISubmodelElement element, Func<ISubmodelElement, SemanticTreeNode?> extractChild)
    {
        var range = (Range)element;
        var semanticId = semanticIdResolver.ExtractSemanticId(range);
        var valueType = semanticIdResolver.GetValueType(range);
        var node = new SemanticBranchNode(semanticId, semanticIdResolver.GetCardinality(range));

        node.AddChild(new SemanticLeafNode(semanticId + SemanticIdResolver.RangeMinimumPostFixSeparator, string.Empty, valueType, Cardinality.ZeroToOne));
        node.AddChild(new SemanticLeafNode(semanticId + SemanticIdResolver.RangeMaximumPostFixSeparator, string.Empty, valueType, Cardinality.ZeroToOne));

        return node;
    }

    public void FillOut(ISubmodelElement element, SemanticTreeNode values, Action<List<ISubmodelElement>, SemanticTreeNode, bool> fillOutChildren)
    {
        var range = (Range)element;

        if (values is not SemanticBranchNode branchNode)
        {
            return;
        }

        var leafNodes = branchNode.Children.OfType<SemanticLeafNode>().ToList();

        range.Min = leafNodes.FirstOrDefault(n => n.SemanticId
                                                   .EndsWith(SemanticIdResolver.RangeMinimumPostFixSeparator, StringComparison.Ordinal))?
                                                   .Value;

        range.Max = leafNodes.FirstOrDefault(n => n.SemanticId
                                                   .EndsWith(SemanticIdResolver.RangeMaximumPostFixSeparator, StringComparison.Ordinal))?
                                                   .Value;
    }
}
