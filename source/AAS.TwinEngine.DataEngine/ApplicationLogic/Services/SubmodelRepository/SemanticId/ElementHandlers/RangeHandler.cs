using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

using Range = AasCore.Aas3_1.Range;

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

        if (!TemplateValueChecker.IsResolved(range.Min))
        {
            node.AddChild(new SemanticLeafNode(semanticId + SemanticIdResolver.RangeMinimumPostFixSeparator, string.Empty, valueType, Cardinality.ZeroToOne));
        }

        if (!TemplateValueChecker.IsResolved(range.Max))
        {
            node.AddChild(new SemanticLeafNode(semanticId + SemanticIdResolver.RangeMaximumPostFixSeparator, string.Empty, valueType, Cardinality.ZeroToOne));
        }

        return node.Children.Count == 0 ? null : node;
    }

    public void FillOut(ISubmodelElement element, SemanticTreeNode values, Action<List<ISubmodelElement>, SemanticTreeNode, bool> fillOutChildren)
    {
        var range = (Range)element;

        if (values is not SemanticBranchNode branchNode)
        {
            return;
        }

        var leafNodes = branchNode.Children.OfType<SemanticLeafNode>().ToList();

        var minLeaf = leafNodes.FirstOrDefault(n => n.SemanticId.EndsWith(SemanticIdResolver.RangeMinimumPostFixSeparator, StringComparison.Ordinal));
        if (minLeaf != null)
        {
            range.Min = (string?)minLeaf.Value;
        }

        var maxLeaf = leafNodes.FirstOrDefault(n => n.SemanticId.EndsWith(SemanticIdResolver.RangeMaximumPostFixSeparator, StringComparison.Ordinal));
        if (maxLeaf != null)
        {
            range.Max = (string?)maxLeaf.Value;
        }
    }
}
