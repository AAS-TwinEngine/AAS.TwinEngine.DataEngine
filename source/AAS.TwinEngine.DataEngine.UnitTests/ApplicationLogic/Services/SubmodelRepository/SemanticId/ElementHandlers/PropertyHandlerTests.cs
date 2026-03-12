using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class PropertyHandlerTests
{
    private readonly PropertyHandler _sut;
    private readonly ISemanticIdResolver _resolver;

    public PropertyHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _sut = new PropertyHandler(_resolver);
    }

    [Fact]
    public void CanHandle_Property_ReturnsTrue()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        True(_sut.CanHandle(property));
    }

    [Fact]
    public void CanHandle_NonProperty_ReturnsFalse()
    {
        var collection = new SubmodelElementCollection(idShort: "Test");

        False(_sut.CanHandle(collection));
    }

    [Fact]
    public void Extract_ReturnsLeafNodeWithSemanticIdAndType()
    {
        var property = new Property(idShort: "MyProp", valueType: DataTypeDefXsd.String, value: "test");
        _resolver.ResolveElementSemanticId(property, "MyProp").Returns("http://test/my-prop");
        _resolver.GetValueType(property).Returns(DataType.String);
        _resolver.GetCardinality(property).Returns(Cardinality.One);

        var result = _sut.Extract(property, _ => null);

        var leaf = IsType<SemanticLeafNode>(result);
        Equal("http://test/my-prop", leaf.SemanticId);
        Equal(DataType.String, leaf.DataType);
        Equal(Cardinality.One, leaf.Cardinality);
    }

    [Fact]
    public void FillOut_WithLeafNode_SetsPropertyValue()
    {
        var property = new Property(idShort: "MyProp", valueType: DataTypeDefXsd.String, value: "");
        var values = new SemanticLeafNode("http://test/my-prop", "NewValue", DataType.String, Cardinality.One);

        _sut.FillOut(property, values, (_, _, _) => { });

        Equal("NewValue", property.Value);
    }

    [Fact]
    public void FillOut_WithBranchNode_DoesNotModifyPropertyValue()
    {
        var property = new Property(idShort: "MyProp", valueType: DataTypeDefXsd.String, value: "original");
        var values = new SemanticBranchNode("http://test/my-prop", Cardinality.One);

        _sut.FillOut(property, values, (_, _, _) => { });

        Equal("original", property.Value);
    }
}
