using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;

/// <summary>
/// Pre-order (Euler tour) index over a semantic value tree. Every node gets an interval
/// [Start, End) so "is descendant of" is an integer range check instead of a materialized
/// descendant list per node.
/// </summary>
internal sealed class SemanticValueIndex
{
    private static readonly IReadOnlyList<SemanticTreeNode> Empty = [];

    private readonly Dictionary<SemanticTreeNode, NodeEntry> _entries;
    private readonly Dictionary<string, List<LeafEntry>> _leavesBySemanticId;

    private SemanticValueIndex(Dictionary<SemanticTreeNode, NodeEntry> entries, Dictionary<string, List<LeafEntry>> leavesBySemanticId)
    {
        _entries = entries;
        _leavesBySemanticId = leavesBySemanticId;
    }

    public static SemanticValueIndex Build(SemanticTreeNode root)
    {
        var entries = new Dictionary<SemanticTreeNode, NodeEntry>(ReferenceEqualityComparer.Instance);
        var leaves = new Dictionary<string, List<LeafEntry>>(StringComparer.Ordinal);

        _ = IndexNode(root, 0, entries, leaves);

        return new SemanticValueIndex(entries, leaves);
    }

    public IReadOnlyList<SemanticTreeNode> GetDirectChildren(SemanticTreeNode node, string semanticId)
        => Lookup(_entries.TryGetValue(node, out var entry) ? entry.DirectChildren : null, semanticId);

    public IReadOnlyList<SemanticTreeNode> GetDirectBranchChildren(SemanticTreeNode node, string semanticId)
        => Lookup(_entries.TryGetValue(node, out var entry) ? entry.DirectBranchChildren : null, semanticId);

    /// <summary>
    /// All leaf nodes with <paramref name="semanticId"/> inside the subtree rooted at
    /// <paramref name="node"/>, including the node itself when it is a matching leaf.
    /// </summary>
    public IReadOnlyList<SemanticTreeNode> GetLeafDescendants(SemanticTreeNode node, string semanticId)
    {
        if (!_entries.TryGetValue(node, out var entry) || !_leavesBySemanticId.TryGetValue(semanticId, out var leaves))
        {
            return Empty;
        }

        var first = LowerBound(leaves, entry.Start);
        if (first >= leaves.Count || leaves[first].Order >= entry.End)
        {
            return Empty;
        }

        var matches = new List<SemanticTreeNode>();
        for (var i = first; i < leaves.Count && leaves[i].Order < entry.End; i++)
        {
            matches.Add(leaves[i].Node);
        }

        return matches;
    }

    private static IReadOnlyList<SemanticTreeNode> Lookup(Dictionary<string, List<SemanticTreeNode>>? index, string semanticId)
        => index is not null && index.TryGetValue(semanticId, out var nodes) ? nodes : Empty;

    private static int IndexNode(
        SemanticTreeNode node,
        int order,
        Dictionary<SemanticTreeNode, NodeEntry> entries,
        Dictionary<string, List<LeafEntry>> leaves)
    {
        var start = order++;

        if (node is SemanticLeafNode leaf)
        {
            if (!leaves.TryGetValue(leaf.SemanticId, out var leafEntries))
            {
                leafEntries = [];
                leaves[leaf.SemanticId] = leafEntries;
            }

            // Pre-order traversal keeps each list sorted by Order, enabling binary search.
            leafEntries.Add(new LeafEntry(start, leaf));
        }

        Dictionary<string, List<SemanticTreeNode>>? directChildren = null;
        Dictionary<string, List<SemanticTreeNode>>? directBranchChildren = null;

        if (node is SemanticBranchNode branch && branch.Children.Count > 0)
        {
            directChildren = new Dictionary<string, List<SemanticTreeNode>>(StringComparer.Ordinal);

            foreach (var child in branch.Children)
            {
                Add(directChildren, child);

                if (child is SemanticBranchNode)
                {
                    directBranchChildren ??= new Dictionary<string, List<SemanticTreeNode>>(StringComparer.Ordinal);
                    Add(directBranchChildren, child);
                }

                order = IndexNode(child, order, entries, leaves);
            }
        }

        entries[node] = new NodeEntry(start, order, directChildren, directBranchChildren);
        return order;
    }

    private static void Add(Dictionary<string, List<SemanticTreeNode>> index, SemanticTreeNode node)
    {
        if (!index.TryGetValue(node.SemanticId, out var nodes))
        {
            nodes = [];
            index[node.SemanticId] = nodes;
        }

        nodes.Add(node);
    }

    private static int LowerBound(List<LeafEntry> leaves, int order)
    {
        var low = 0;
        var high = leaves.Count;

        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (leaves[mid].Order < order)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    private readonly record struct NodeEntry(
        int Start,
        int End,
        Dictionary<string, List<SemanticTreeNode>>? DirectChildren,
        Dictionary<string, List<SemanticTreeNode>>? DirectBranchChildren);

    private readonly record struct LeafEntry(int Order, SemanticLeafNode Node);
}
