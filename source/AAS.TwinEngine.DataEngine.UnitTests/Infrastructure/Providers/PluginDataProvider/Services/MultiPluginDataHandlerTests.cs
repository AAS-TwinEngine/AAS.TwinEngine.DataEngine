using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Services;

public class MultiPluginDataHandlerTests
{
    private readonly MultiPluginDataHandler _sut;
    private readonly ILogger<MultiPluginDataHandler> _logger;

    public MultiPluginDataHandlerTests()
    {
        var semantics = Substitute.For<IOptions<Semantics>>();
        _logger = Substitute.For<ILogger<MultiPluginDataHandler>>();
        semantics.Value.Returns(new Semantics { SubmodelElementIndexContextPrefix = "_index_" });
        _sut = new MultiPluginDataHandler(semantics, _logger);
    }

    [Fact]
    public void HasIndex_ShouldReturnTrue_WhenSemanticIdContainsIndex()
    {
        var result = _sut.HasIndex("abc_index_01");
        Assert.True(result);
    }

    [Fact]
    public void HasIndex_ShouldReturnFalse_WhenSemanticIdDoesNotContainIndex()
    {
        var result = _sut.HasIndex("abc");
        Assert.False(result);
    }

    [Fact]
    public void GetSemanticId_ShouldReturnTrimmed_WhenIndexPresent()
    {
        var result = _sut.GetSemanticId("abc_index_01");
        Assert.Equal("abc", result);
    }

    [Fact]
    public void GetSemanticId_ShouldReturnSame_WhenNoIndex()
    {
        var result = _sut.GetSemanticId("abc");
        Assert.Equal("abc", result);
    }

    [Fact]
    public void SplitByPluginManifests_ShouldFilterBySupportedIds()
    {
        var globalTree = new SemanticBranchNode("root", Cardinality.One);
        globalTree.AddChild(new SemanticLeafNode("abc", "val1", DataType.String, Cardinality.One));
        globalTree.AddChild(new SemanticLeafNode("def", "val2", DataType.String, Cardinality.One));

        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin1",
                PluginUrl = new Uri("https://example.com/plugin1"),
                SupportedSemanticIds = new List<string> { "abc" },
                Capabilities = new Capabilities { HasShellDescriptor = true }
            },
            new()
            {
                PluginName = "TestPlugin2",
                PluginUrl = new Uri("https://example.com/plugin2"),
                SupportedSemanticIds = new List<string> { "def" },
                Capabilities = new Capabilities { HasShellDescriptor = false }
            }
        };

        var result = _sut.SplitByPluginManifests(globalTree, manifests);

        Assert.Equal(2, result.Count);
        Assert.Contains("abc", ((SemanticBranchNode)result["TestPlugin1"]).Children[0].SemanticId);
        Assert.Contains("def", ((SemanticBranchNode)result["TestPlugin2"]).Children[0].SemanticId);
    }

    [Theory]
    [InlineData(Cardinality.One)]
    [InlineData(Cardinality.OneToMany)]
    public void SplitByPluginManifests_ShouldThrow_WhenRequiredSemanticIdNotSupported(Cardinality cardinality)
    {
        var globalTree = new SemanticBranchNode("root", Cardinality.One);
        globalTree.AddChild(new SemanticLeafNode("abc", "val1", DataType.String, cardinality));
        globalTree.AddChild(new SemanticLeafNode("def", "val2", DataType.String, Cardinality.One));
        globalTree.AddChild(new SemanticLeafNode("xyz", "val2", DataType.String, Cardinality.One));

        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin1",
                PluginUrl = new Uri("https://example.com/plugin1"),
                SupportedSemanticIds = new List<string> { "xyz" },
                Capabilities = new Capabilities { HasShellDescriptor = false }
            },
            new()
            {
                PluginName = "TestPlugin2",
                PluginUrl = new Uri("https://example.com/plugin2"),
                SupportedSemanticIds = new List<string> { "def" },
                Capabilities = new Capabilities { HasShellDescriptor = false }
            }
        };

        Assert.Throws<MultiPluginConflictException>(() => _sut.SplitByPluginManifests(globalTree, manifests));

        _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString()!.Contains("abc")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
    }

    [Fact]
    public void SplitByPluginManifests_ShouldFilter_WhenNoRequiredIdMissing()
    {
        var globalTree = new SemanticBranchNode("root", Cardinality.One);
        globalTree.AddChild(new SemanticLeafNode("abc", "val1", DataType.String, Cardinality.One));
        globalTree.AddChild(new SemanticLeafNode("def", "val2", DataType.String, Cardinality.Unknown));
        globalTree.AddChild(new SemanticLeafNode("xyz", "val2", DataType.String, Cardinality.One));

        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin1",
                PluginUrl = new Uri("https://example.com/plugin1"),
                SupportedSemanticIds = new List<string> { "abc" },
                Capabilities = new Capabilities { HasShellDescriptor = true }
            },
            new()
            {
                PluginName = "TestPlugin2",
                PluginUrl = new Uri("https://example.com/plugin2"),
                SupportedSemanticIds = new List<string> { "xyz" },
                Capabilities = new Capabilities { HasShellDescriptor = false }
            }
        };

        var result = _sut.SplitByPluginManifests(globalTree, manifests);

        Assert.Equal(2, result.Count);
        Assert.Contains("abc", ((SemanticBranchNode)result["TestPlugin1"]).Children[0].SemanticId);
        Assert.Contains("xyz", ((SemanticBranchNode)result["TestPlugin2"]).Children[0].SemanticId);
    }

    [Fact]
    public void Merge_ShouldHandleLeaf_ZeroToOne()
    {
        var globalLeaf = new SemanticLeafNode("abc", null!, DataType.String, Cardinality.ZeroToOne);
        var valueLeaf = new SemanticLeafNode("abc", "value1", DataType.String, Cardinality.ZeroToOne);

        var result = _sut.Merge(globalLeaf, new List<SemanticTreeNode> { valueLeaf });

        var merged = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("value1", merged.Value);
    }

    [Fact]
    public void Merge_ShouldHandleLeaf_OneToMany()
    {
        var globalLeaf = new SemanticLeafNode("abc", null!, DataType.String, Cardinality.OneToMany);
        var values = new List<SemanticTreeNode>
        {
            new SemanticLeafNode("abc", "v1", DataType.String, Cardinality.OneToMany),
            new SemanticLeafNode("abc", "v2", DataType.String, Cardinality.OneToMany)
        };

        var result = _sut.Merge(globalLeaf, values);
        var merged = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(new List<string> { "v1", "v2" }, merged.Value);
    }

    [Fact]
    public void Merge_ShouldHandleLeaf_Unknown_WithMany()
    {
        var globalLeaf = new SemanticLeafNode("abc", null!, DataType.String, Cardinality.Unknown);
        var values = new List<SemanticTreeNode>
        {
            new SemanticLeafNode("abc", "v1", DataType.String, Cardinality.OneToMany),
            new SemanticLeafNode("abc", "v2", DataType.String, Cardinality.OneToMany)
        };

        var result = _sut.Merge(globalLeaf, values);
        var merged = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(new List<string> { "v1", "v2" }, merged.Value);
    }

    [Fact]
    public void Merge_ShouldHandleLeaf_Unknown_Single()
    {
        var globalLeaf = new SemanticLeafNode("abc", null!, DataType.String, Cardinality.Unknown);
        var values = new List<SemanticTreeNode>
        {
            new SemanticLeafNode("abc", "single", DataType.String, Cardinality.One)
        };

        var result = _sut.Merge(globalLeaf, values);
        var merged = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("single", merged.Value);
    }

    [Fact]
    public void Merge_ShouldHandleBranch_ZeroToOne()
    {
        var globalBranch = new SemanticBranchNode("branch", Cardinality.ZeroToOne);
        globalBranch.AddChild(new SemanticLeafNode("leaf", null!, DataType.String, Cardinality.One));

        var valueBranch = new SemanticBranchNode("branch", Cardinality.ZeroToOne);
        valueBranch.AddChild(new SemanticLeafNode("leaf", "val", DataType.String, Cardinality.One));

        var result = _sut.Merge(globalBranch, new List<SemanticTreeNode> { valueBranch });

        var merged = Assert.IsType<SemanticBranchNode>(result);
        Assert.Single(merged.Children);
    }

    [Fact]
    public void Merge_ShouldHandleBranch_OneToMany()
    {
        var globalParentBranch = new SemanticBranchNode("parentBranch", Cardinality.OneToMany);
        var globalBranch = new SemanticBranchNode("contact", Cardinality.OneToMany);
        globalParentBranch.AddChild(globalBranch);
        globalBranch.AddChild(new SemanticLeafNode("leaf", null!, DataType.String, Cardinality.One));

        var valueParentBranch1 = new SemanticBranchNode("parentBranch", Cardinality.OneToMany);
        var valueBranch1 = new SemanticBranchNode("contact", Cardinality.OneToMany);
        valueParentBranch1.AddChild(valueBranch1);
        valueBranch1.AddChild(new SemanticLeafNode("leaf", "v1", DataType.String, Cardinality.One));

        var valueParentBranch2 = new SemanticBranchNode("parentBranch", Cardinality.OneToMany);
        var valueBranch2 = new SemanticBranchNode("contact", Cardinality.OneToMany);
        valueParentBranch2.AddChild(valueBranch2);
        valueBranch2.AddChild(new SemanticLeafNode("leaf", "v2", DataType.String, Cardinality.One));

        var result = _sut.Merge(valueParentBranch1, new List<SemanticTreeNode> { valueParentBranch1, valueParentBranch2 });

        var merged = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(2, merged.Children.Count);
    }

    [Fact]
    public void Merge_ShouldCreateEmptyBranch_WhenNoCandidates()
    {
        var global = new SemanticBranchNode("branch", Cardinality.ZeroToOne);
        global.AddChild(new SemanticLeafNode("leaf", null!, DataType.String, Cardinality.One));

        var valueTree = new SemanticBranchNode("root", Cardinality.One);

        var result = _sut.Merge(global, new List<SemanticTreeNode> { valueTree });

        var merged = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("branch", merged.SemanticId);
        Assert.Empty(merged.Children);
    }

    [Fact]
    public void Merge_ShouldMergeChildren_WhenCardinalityOne()
    {
        var globalParent = new SemanticBranchNode("parent", Cardinality.One);
        var globalBranch = new SemanticBranchNode("branch", Cardinality.One);
        globalBranch.AddChild(new SemanticLeafNode("leaf", null!, DataType.String, Cardinality.One));
        globalParent.AddChild(globalBranch);

        var valueParent = new SemanticBranchNode("parent", Cardinality.One);
        var candidate = new SemanticBranchNode("branch", Cardinality.One);
        candidate.AddChild(new SemanticLeafNode("leaf", "v1", DataType.String, Cardinality.One));
        candidate.AddChild(new SemanticLeafNode("leaf", "v2", DataType.String, Cardinality.One));
        valueParent.AddChild(candidate);

        var result = _sut.Merge(globalParent, new List<SemanticTreeNode> { valueParent });

        var mergedParent = Assert.IsType<SemanticBranchNode>(result);
        var mergedBranch = Assert.IsType<SemanticBranchNode>(mergedParent.Children[0]);
        Assert.Equal(2, mergedBranch.Children.Count);
    }

    [Fact]
    public void Merge_ShouldReturnBranches_WhenCardinalityZeroToMany()
    {
        var globalParent = new SemanticBranchNode("parent", Cardinality.One);
        var globalBranch = new SemanticBranchNode("branch", Cardinality.ZeroToMany);
        globalBranch.AddChild(new SemanticLeafNode("leaf", null!, DataType.String, Cardinality.One));
        globalParent.AddChild(globalBranch);

        var valueParent = new SemanticBranchNode("parent", Cardinality.One);
        var candidate1 = new SemanticBranchNode("branch", Cardinality.ZeroToMany);
        candidate1.AddChild(new SemanticLeafNode("leaf", "v1", DataType.String, Cardinality.One));
        var candidate2 = new SemanticBranchNode("branch", Cardinality.ZeroToMany);
        candidate2.AddChild(new SemanticLeafNode("leaf", "v2", DataType.String, Cardinality.One));
        valueParent.AddChild(candidate1);
        valueParent.AddChild(candidate2);

        var result = _sut.Merge(globalParent, new List<SemanticTreeNode> { valueParent });

        var mergedParent = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(2, mergedParent.Children.Count);
    }

    [Fact]
    public void Merge_ShouldReturnBranches_WhenCardinalityUnknownAndCandidatesAreMany()
    {
        var globalParent = new SemanticBranchNode("parent", Cardinality.One);
        var globalBranch = new SemanticBranchNode("branch", Cardinality.Unknown);
        globalBranch.AddChild(new SemanticLeafNode("leaf", null!, DataType.String, Cardinality.One));
        globalParent.AddChild(globalBranch);

        var valueParent = new SemanticBranchNode("parent", Cardinality.One);
        var candidate1 = new SemanticBranchNode("branch", Cardinality.OneToMany);
        candidate1.AddChild(new SemanticLeafNode("leaf", "v1", DataType.String, Cardinality.One));
        var candidate2 = new SemanticBranchNode("branch", Cardinality.ZeroToMany);
        candidate2.AddChild(new SemanticLeafNode("leaf", "v2", DataType.String, Cardinality.One));
        valueParent.AddChild(candidate1);
        valueParent.AddChild(candidate2);

        var result = _sut.Merge(globalParent, new List<SemanticTreeNode> { valueParent });

        var mergedParent = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(2, mergedParent.Children.Count);
    }

    [Fact]
    public void Merge_ShouldMergeChildren_WhenCardinalityUnknownAndCandidatesNotMany()
    {
        var globalParent = new SemanticBranchNode("parent", Cardinality.One);
        var globalBranch = new SemanticBranchNode("branch", Cardinality.Unknown);
        globalBranch.AddChild(new SemanticLeafNode("leaf", null!, DataType.String, Cardinality.One));
        globalParent.AddChild(globalBranch);

        var valueParent = new SemanticBranchNode("parent", Cardinality.One);
        var candidate1 = new SemanticBranchNode("branch", Cardinality.One);
        candidate1.AddChild(new SemanticLeafNode("leaf", "v1", DataType.String, Cardinality.One));
        var candidate2 = new SemanticBranchNode("branch", Cardinality.One);
        candidate2.AddChild(new SemanticLeafNode("leaf", "v2", DataType.String, Cardinality.One));
        valueParent.AddChild(candidate1);
        valueParent.AddChild(candidate2);

        var result = _sut.Merge(globalParent, new List<SemanticTreeNode> { valueParent });

        var mergedParent = Assert.IsType<SemanticBranchNode>(result);
        var mergedBranch = Assert.IsType<SemanticBranchNode>(mergedParent.Children[0]);
        Assert.Equal(2, mergedBranch.Children.Count);
    }

    [Fact]
    public void Merge_ShouldThrowInternalDataProcessingException_ForUnknownNodeType()
    {
        var dummyNode = new DummyNode("x", Cardinality.One);

        Assert.Throws<InternalDataProcessingException>(() =>
            _sut.Merge(dummyNode, new List<SemanticTreeNode>()));
    }

    private class DummyNode(string semanticId, Cardinality cardinality) : SemanticTreeNode(semanticId, cardinality);
}
