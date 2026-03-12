using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public class ReferenceHelperTests
{
    private readonly ReferenceHelper _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly ILogger<ReferenceHelper> _logger;

    public ReferenceHelperTests()
    {
        var semantics = Options.Create(new Semantics
        {
            MultiLanguageSemanticPostfixSeparator = "_",
            SubmodelElementIndexContextPrefix = "_aastwinengineindex_"
        });
        _resolver = new SemanticIdResolver(semantics);
        _logger = Substitute.For<ILogger<ReferenceHelper>>();
        _sut = new ReferenceHelper(_resolver, _logger);
    }

    [Fact]
    public void ExtractReferenceKeys_WithKeys_ReturnsBranchNode()
    {
        var reference = new Reference(
            ReferenceTypes.ModelReference,
            [
                new Key(KeyTypes.Submodel, "submodel-value"),
                new Key(KeyTypes.Property, "prop-value"),
            ]
        );

        var result = _sut.ExtractReferenceKeys(reference, "http://test/ref", Cardinality.One);

        NotNull(result);
        Equal("http://test/ref", result!.SemanticId);
        Equal(2, result.Children.Count);
        var submodelLeaf = IsType<SemanticLeafNode>(result.Children[0]);
        Equal("http://test/ref_Submodel", submodelLeaf.SemanticId);
        var propLeaf = IsType<SemanticLeafNode>(result.Children[1]);
        Equal("http://test/ref_Property", propLeaf.SemanticId);
    }

    [Fact]
    public void ExtractReferenceKeys_WithMultipleSameType_IncludesIndex()
    {
        var reference = new Reference(
            ReferenceTypes.ModelReference,
            [
                new Key(KeyTypes.SubmodelElementCollection, "col0"),
                new Key(KeyTypes.SubmodelElementCollection, "col1"),
            ]
        );

        var result = _sut.ExtractReferenceKeys(reference, "http://test/ref", Cardinality.One);

        NotNull(result);
        Equal(2, result!.Children.Count);
        var leaf0 = IsType<SemanticLeafNode>(result.Children[0]);
        Equal("http://test/ref_SubmodelElementCollection_0", leaf0.SemanticId);
        var leaf1 = IsType<SemanticLeafNode>(result.Children[1]);
        Equal("http://test/ref_SubmodelElementCollection_1", leaf1.SemanticId);
    }

    [Fact]
    public void ExtractReferenceKeys_EmptyKeys_ReturnsNull()
    {
        var reference = new Reference(ReferenceTypes.ModelReference, []);

        var result = _sut.ExtractReferenceKeys(reference, "http://test/ref", Cardinality.One);

        Null(result);
    }

    [Fact]
    public void PopulateReferenceKeys_WithMatchingLeafNodes_UpdatesKeyValues()
    {
        var reference = new Reference(
            ReferenceTypes.ModelReference,
            [
                new Key(KeyTypes.Submodel, ""),
                new Key(KeyTypes.Property, ""),
            ]
        );

        var branchNode = new SemanticBranchNode("http://test/ref", Cardinality.One);
        branchNode.AddChild(new SemanticLeafNode("http://test/ref_Submodel", "NewSubmodelValue", DataType.String, Cardinality.One));
        branchNode.AddChild(new SemanticLeafNode("http://test/ref_Property", "NewPropValue", DataType.String, Cardinality.One));

        _sut.PopulateReferenceKeys(reference, branchNode, "http://test/ref");

        Equal("NewSubmodelValue", reference.Keys[0].Value);
        Equal("NewPropValue", reference.Keys[1].Value);
    }

    [Fact]
    public void PopulateReferenceKeys_WithNonBranchNode_LogsWarning()
    {
        var reference = new Reference(
            ReferenceTypes.ModelReference,
            [new Key(KeyTypes.Submodel, "original")]
        );
        var leafNode = new SemanticLeafNode("http://test/ref", "val", DataType.String, Cardinality.One);

        _sut.PopulateReferenceKeys(reference, leafNode, "http://test/ref");

        Equal("original", reference.Keys[0].Value);
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("Expected SemanticBranchNode")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }

    [Fact]
    public void PopulateReferenceKeys_EmptyKeys_LogsInfo()
    {
        var reference = new Reference(ReferenceTypes.ModelReference, []);
        var branchNode = new SemanticBranchNode("http://test/ref", Cardinality.One);

        _sut.PopulateReferenceKeys(reference, branchNode, "http://test/ref");

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("has no keys")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }

    [Fact]
    public void PopulateReferenceKeys_MissingLeafNode_LogsWarning()
    {
        var reference = new Reference(
            ReferenceTypes.ModelReference,
            [new Key(KeyTypes.Submodel, "original")]
        );
        var branchNode = new SemanticBranchNode("http://test/ref", Cardinality.One);

        _sut.PopulateReferenceKeys(reference, branchNode, "http://test/ref");

        Equal("original", reference.Keys[0].Value);
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("No matching leaf node")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }

    [Fact]
    public void PopulateRelationshipReference_ExternalReference_DoesNotModify()
    {
        var reference = new Reference(
            ReferenceTypes.ExternalReference,
            [new Key(KeyTypes.GlobalReference, "original")]
        );
        var tree = new SemanticBranchNode("http://test", Cardinality.One);

        _sut.PopulateRelationshipReference(reference, tree, "http://test", "_first");

        Equal("original", reference.Keys[0].Value);
    }

    [Fact]
    public void PopulateRelationshipReference_ModelReference_WithMatchingNode_PopulatesKeys()
    {
        var reference = new Reference(
            ReferenceTypes.ModelReference,
            [new Key(KeyTypes.Submodel, "")]
        );

        var firstBranch = new SemanticBranchNode("http://test_first", Cardinality.One);
        firstBranch.AddChild(new SemanticLeafNode("http://test_first_Submodel", "NewValue", DataType.String, Cardinality.One));

        var tree = new SemanticBranchNode("http://test", Cardinality.One);
        tree.AddChild(firstBranch);

        _sut.PopulateRelationshipReference(reference, tree, "http://test", "_first");

        Equal("NewValue", reference.Keys[0].Value);
    }

    [Fact]
    public void PopulateRelationshipReference_ModelReference_NoMatchingNode_LogsWarning()
    {
        var reference = new Reference(
            ReferenceTypes.ModelReference,
            [new Key(KeyTypes.Submodel, "original")]
        );
        var tree = new SemanticBranchNode("http://test", Cardinality.One);

        _sut.PopulateRelationshipReference(reference, tree, "http://test", "_first");

        Equal("original", reference.Keys[0].Value);
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("No matching node")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }
}
