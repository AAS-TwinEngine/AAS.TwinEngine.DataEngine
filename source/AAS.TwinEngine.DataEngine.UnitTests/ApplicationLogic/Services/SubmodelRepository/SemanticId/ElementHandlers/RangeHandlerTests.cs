using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using NSubstitute;

using static Xunit.Assert;

using Range = AasCore.Aas3_0.Range;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class RangeHandlerTests
{
    private readonly RangeHandler _sut;
    private readonly ISemanticIdResolver _resolver;

    public RangeHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _sut = new RangeHandler(_resolver);
    }

    [Fact]
    public void CanHandle_Range_ReturnsTrue()
    {
        var range = new Range(valueType: DataTypeDefXsd.Double, idShort: "Test");

        True(_sut.CanHandle(range));
    }

    [Fact]
    public void CanHandle_NonRange_ReturnsFalse()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        False(_sut.CanHandle(property));
    }

    [Fact]
    public void Extract_ReturnsBranchWithMinAndMaxLeaves()
    {
        var range = new Range(valueType: DataTypeDefXsd.Double, idShort: "TestRange");
        _resolver.ExtractSemanticId(range).Returns("http://test/range");
        _resolver.GetValueType(range).Returns(DataType.Number);
        _resolver.GetCardinality(range).Returns(Cardinality.One);

        var result = _sut.Extract(range, _ => null);

        var branch = IsType<SemanticBranchNode>(result);
        Equal("http://test/range", branch.SemanticId);
        Equal(2, branch.Children.Count);
        var minLeaf = IsType<SemanticLeafNode>(branch.Children[0]);
        Equal("http://test/range" + SemanticIdResolver.RangeMinimumPostFixSeparator, minLeaf.SemanticId);
        Equal(DataType.Number, minLeaf.DataType);
        var maxLeaf = IsType<SemanticLeafNode>(branch.Children[1]);
        Equal("http://test/range" + SemanticIdResolver.RangeMaximumPostFixSeparator, maxLeaf.SemanticId);
        Equal(DataType.Number, maxLeaf.DataType);
    }

    [Fact]
    public void FillOut_WithBranchNode_SetsMinAndMax()
    {
        var range = new Range(valueType: DataTypeDefXsd.Double, idShort: "TestRange");
        var branchNode = new SemanticBranchNode("http://test/range", Cardinality.One);
        branchNode.AddChild(new SemanticLeafNode("http://test/range_min", "10.5", DataType.Number, Cardinality.One));
        branchNode.AddChild(new SemanticLeafNode("http://test/range_max", "99.9", DataType.Number, Cardinality.One));

        _sut.FillOut(range, branchNode, (_, _, _) => { });

        Equal("10.5", range.Min);
        Equal("99.9", range.Max);
    }

    [Fact]
    public void FillOut_WithLeafNode_DoesNotSetMinMax()
    {
        var range = new Range(valueType: DataTypeDefXsd.Double, idShort: "TestRange", min: "0", max: "100");
        var leafNode = new SemanticLeafNode("http://test/range", "val", DataType.Number, Cardinality.One);

        _sut.FillOut(range, leafNode, (_, _, _) => { });

        Equal("0", range.Min);
        Equal("100", range.Max);
    }

    [Fact]
    public void FillOut_WithMissingMinLeaf_SetsMinToNull()
    {
        var range = new Range(valueType: DataTypeDefXsd.Double, idShort: "TestRange");
        var branchNode = new SemanticBranchNode("http://test/range", Cardinality.One);
        branchNode.AddChild(new SemanticLeafNode("http://test/range_max", "99.9", DataType.Number, Cardinality.One));

        _sut.FillOut(range, branchNode, (_, _, _) => { });

        Null(range.Min);
        Equal("99.9", range.Max);
    }
}
