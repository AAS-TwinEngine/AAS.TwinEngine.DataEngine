using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class ListHandlerTests
{
    private readonly ListHandler _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly ILogger<ListHandler> _logger;

    public ListHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _logger = Substitute.For<ILogger<ListHandler>>();
        _sut = new ListHandler(_resolver, _logger);
    }

    [Fact]
    public void CanHandle_SubmodelElementList_ReturnsTrue()
    {
        var list = new SubmodelElementList(idShort: "Test", typeValueListElement: AasSubmodelElements.Property);

        True(_sut.CanHandle(list));
    }

    [Fact]
    public void CanHandle_NonList_ReturnsFalse()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        False(_sut.CanHandle(property));
    }

    [Fact]
    public void Extract_WithChildren_ReturnsBranchNodeWithChildren()
    {
        var child = new Property(idShort: "Item", valueType: DataTypeDefXsd.String);
        var list = new SubmodelElementList(
            idShort: "MyList",
            typeValueListElement: AasSubmodelElements.Property,
            value: [child]
        );
        _resolver.ResolveElementSemanticId(list, "MyList").Returns("http://test/list");
        _resolver.GetCardinality(list).Returns(Cardinality.ZeroToMany);

        var childNode = new SemanticLeafNode("http://test/item", "", DataType.String, Cardinality.One);

        var result = _sut.Extract(list, _ => childNode);

        var branch = IsType<SemanticBranchNode>(result);
        Equal("http://test/list", branch.SemanticId);
        Single(branch.Children);
    }

    [Fact]
    public void Extract_WithNullValue_LogsWarningAndReturnsEmptyBranch()
    {
        var list = new SubmodelElementList(
            idShort: "EmptyList",
            typeValueListElement: AasSubmodelElements.Property,
            value: null
        );
        _resolver.ResolveElementSemanticId(list, "EmptyList").Returns("http://test/empty");
        _resolver.GetCardinality(list).Returns(Cardinality.Unknown);

        var result = _sut.Extract(list, _ => null);

        var branch = IsType<SemanticBranchNode>(result);
        Empty(branch.Children);
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("No elements defined in SubmodelElementList EmptyList")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }

    [Fact]
    public void FillOut_WithChildren_DelegatesToFillOutChildren()
    {
        var child = new Property(idShort: "Item", valueType: DataTypeDefXsd.String);
        var list = new SubmodelElementList(
            idShort: "List",
            typeValueListElement: AasSubmodelElements.Property,
            value: [child]
        );
        var values = new SemanticBranchNode("http://test/list", Cardinality.One);
        var fillOutCalled = false;

        _sut.FillOut(list, values, (elements, node, updateIdShort) =>
        {
            fillOutCalled = true;
            False(updateIdShort);
        });

        True(fillOutCalled);
    }

    [Fact]
    public void FillOut_WithNullValue_DoesNotCallFillOutChildren()
    {
        var list = new SubmodelElementList(
            idShort: "List",
            typeValueListElement: AasSubmodelElements.Property,
            value: null
        );
        var values = new SemanticBranchNode("http://test/list", Cardinality.One);

        _sut.FillOut(list, values, (_, _, _) => Fail("Should not be called"));
    }
}
