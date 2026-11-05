using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class CollectionHandlerTests
{
    private readonly CollectionHandler _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly ILogger<CollectionHandler> _logger;

    public CollectionHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _logger = Substitute.For<ILogger<CollectionHandler>>();
        _sut = new CollectionHandler(_resolver, _logger);
    }

    [Fact]
    public void CanHandle_Collection_ReturnsTrue()
    {
        var collection = new SubmodelElementCollection(idShort: "Test");

        True(_sut.CanHandle(collection));
    }

    [Fact]
    public void CanHandle_NonCollection_ReturnsFalse()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        False(_sut.CanHandle(property));
    }

    [Fact]
    public void Extract_WithChildren_ReturnsBranchNodeWithChildren()
    {
        var child = new Property(idShort: "Child", valueType: DataTypeDefXsd.String);
        var collection = new SubmodelElementCollection(idShort: "MyCollection", value: [child]);
        _resolver.ResolveElementSemanticId(collection, "MyCollection").Returns("http://test/collection");
        _resolver.GetCardinality(collection).Returns(Cardinality.ZeroToMany);

        var childNode = new SemanticLeafNode("http://test/child", "", DataType.String, Cardinality.One);
        SemanticTreeNode? extractChild(ISubmodelElement _) => childNode;

        var result = _sut.Extract(collection, extractChild);

        var branch = IsType<SemanticBranchNode>(result);
        Equal("http://test/collection", branch.SemanticId);
        Equal(Cardinality.ZeroToMany, branch.Cardinality);
        Single(branch.Children);
    }

    [Fact]
    public void Extract_AllChildrenResolvedInTemplate_ReturnsNull()
    {
        var collection = new SubmodelElementCollection(
            idShort: "ContactInformation",
            value: [new Property(idShort: "Company", valueType: DataTypeDefXsd.String, value: "M&M")]);
        _resolver.ResolveElementSemanticId(collection, "ContactInformation").Returns("http://test/contact");
        _resolver.GetCardinality(collection).Returns(Cardinality.ZeroToMany);

        Null(_sut.Extract(collection, _ => null));
    }

    [Fact]
    public void Extract_SomeChildrenResolvedInTemplate_ReturnsBranchWithUnresolvedChildrenOnly()
    {
        var name = new Property(idShort: "Name", valueType: DataTypeDefXsd.String);
        var company = new Property(idShort: "Company", valueType: DataTypeDefXsd.String, value: "M&M");
        var collection = new SubmodelElementCollection(idShort: "ContactInformation", value: [name, company]);
        _resolver.ResolveElementSemanticId(collection, "ContactInformation").Returns("http://test/contact");
        _resolver.GetCardinality(collection).Returns(Cardinality.ZeroToMany);
        var nameNode = new SemanticLeafNode("http://test/name", "", DataType.String, Cardinality.One);

        var result = _sut.Extract(collection, child => ReferenceEquals(child, name) ? nameNode : null);

        var branch = IsType<SemanticBranchNode>(result);
        Same(nameNode, Single(branch.Children));
    }

    [Fact]
    public void Extract_WithNullValue_ReturnsBranchNodeAndLogsWarning()
    {
        var collection = new SubmodelElementCollection(idShort: "EmptyCollection", value: null);
        _resolver.ResolveElementSemanticId(collection, "EmptyCollection").Returns("http://test/empty");
        _resolver.GetCardinality(collection).Returns(Cardinality.Unknown);

        var result = _sut.Extract(collection, _ => null);

        var branch = IsType<SemanticBranchNode>(result);
        Empty(branch.Children);
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("No elements defined in SubmodelElementCollection EmptyCollection")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }

    [Fact]
    public void Extract_WithEmptyValue_ReturnsBranchNodeAndLogsWarning()
    {
        var collection = new SubmodelElementCollection(idShort: "EmptyCollection", value: []);
        _resolver.ResolveElementSemanticId(collection, "EmptyCollection").Returns("http://test/empty");
        _resolver.GetCardinality(collection).Returns(Cardinality.Unknown);

        var result = _sut.Extract(collection, _ => null);

        var branch = IsType<SemanticBranchNode>(result);
        Empty(branch.Children);
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("No elements defined in SubmodelElementCollection EmptyCollection")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }

    [Fact]
    public void FillOut_WithChildren_DelegatesToFillOutChildren()
    {
        var child = new Property(idShort: "Child", valueType: DataTypeDefXsd.String);
        var collection = new SubmodelElementCollection(idShort: "Col", value: [child]);
        var values = new SemanticBranchNode("http://test/col", Cardinality.One);
        var fillOutCalled = false;

        _sut.FillOut(collection, values, (elements, node, updateIdShort) =>
        {
            fillOutCalled = true;
            True(updateIdShort);
            Same(collection.Value, elements);
        });

        True(fillOutCalled);
    }

    [Fact]
    public void FillOut_WithNullValue_DoesNotCallFillOutChildren()
    {
        var collection = new SubmodelElementCollection(idShort: "Col", value: null);
        var values = new SemanticBranchNode("http://test/col", Cardinality.One);

        _sut.FillOut(collection, values, (_, _, _) => Fail("Should not be called"));
    }

    [Fact]
    public void FillOut_WithEmptyValue_DoesNotCallFillOutChildren()
    {
        var collection = new SubmodelElementCollection(idShort: "Col", value: []);
        var values = new SemanticBranchNode("http://test/col", Cardinality.One);

        _sut.FillOut(collection, values, (_, _, _) => Fail("Should not be called"));
    }
}
