using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;

public class SubmodelFillerTests
{
    private readonly SubmodelFiller _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly ISubmodelElementHelper _elementHelper;
    private readonly ILogger<SubmodelFiller> _logger;
    private readonly List<ISubmodelElementTypeHandler> _handlers;

    public SubmodelFillerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _elementHelper = Substitute.For<ISubmodelElementHelper>();
        _logger = Substitute.For<ILogger<SubmodelFiller>>();
        _handlers = [];
        _sut = new SubmodelFiller(_resolver, _elementHelper, _handlers, _logger);
    }

    [Fact]
    public void FillOutTemplate_NullSubmodel_ThrowsArgumentNullException()
    {
        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        Throws<ArgumentNullException>(() => _sut.FillOutTemplate(null!, values));
    }

    [Fact]
    public void FillOutTemplate_NullValues_ThrowsArgumentNullException()
    {
        var submodel = Substitute.For<ISubmodel>();
        submodel.SubmodelElements.Returns([]);

        Throws<ArgumentNullException>(() => _sut.FillOutTemplate(submodel, null!));
    }

    [Fact]
    public void FillOutTemplate_NullSubmodelElements_ThrowsArgumentNullException()
    {
        var submodel = Substitute.For<ISubmodel>();
        submodel.SubmodelElements.Returns((List<ISubmodelElement>?)null);
        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        Throws<ArgumentNullException>(() => _sut.FillOutTemplate(submodel, values));
    }

    [Fact]
    public void FillOutTemplate_NoMatchingNodes_PreservesElements()
    {
        var property = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);
        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { property };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(property).Returns("http://test/prop");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Single(elements);
    }

    [Fact]
    public void FillOutElement_NullElement_ThrowsArgumentNullException()
    {
        var values = new SemanticLeafNode("test", "val", DataType.String, Cardinality.One);

        Throws<ArgumentNullException>(() => _sut.FillOutElement(null!, values));
    }

    [Fact]
    public void FillOutElement_NullValues_ThrowsArgumentNullException()
    {
        var element = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);

        Throws<ArgumentNullException>(() => _sut.FillOutElement(element, null!));
    }

    [Fact]
    public void FillOutElement_NoMatchingHandler_ThrowsException()
    {
        var element = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);
        var values = new SemanticLeafNode("test", "val", DataType.String, Cardinality.One);

        var ex = Throws<InternalDataProcessingException>(() => _sut.FillOutElement(element, values));
        Equal("Internal Server Error.", ex.Message);
    }

    [Fact]
    public void FillOutElement_WithMatchingHandler_DelegatesToHandler()
    {
        var element = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);
        var values = new SemanticLeafNode("test", "val", DataType.String, Cardinality.One);

        var handler = Substitute.For<ISubmodelElementTypeHandler>();
        handler.CanHandle(element).Returns(true);
        _handlers.Add(handler);

        _sut.FillOutElement(element, values);

        handler.Received(1).FillOut(element, values, Arg.Any<Action<List<ISubmodelElement>, SemanticTreeNode, bool>>());
    }

    [Fact]
    public void FillOutTemplate_RemovesInternalSemanticIdQualifier_FromProperty()
    {
        var property = new Property(
            idShort: "Prop",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/internal")
            ]);
        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { property };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(property).Returns("http://test/prop");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Empty(property.Qualifiers!);
    }

    [Fact]
    public void FillOutTemplate_PreservesNonInternalQualifiers_WhenRemovingInternalSemanticId()
    {
        var property = new Property(
            idShort: "Prop",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "ExternalReference", valueType: DataTypeDefXsd.String, value: "ZeroToOne"),
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/internal")
            ]);
        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { property };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(property).Returns("http://test/prop");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Single(property.Qualifiers!);
        Equal("ExternalReference", property.Qualifiers[0].Type);
    }

    [Fact]
    public void FillOutTemplate_RemovesInternalSemanticIdQualifier_FromNestedCollection()
    {
        var innerProperty = new Property(
            idShort: "InnerProp",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/inner")
            ]);
        var collection = new SubmodelElementCollection(
            idShort: "Collection",
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/collection")
            ],
            value: [innerProperty]);

        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { collection };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(collection).Returns("http://test/collection");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Empty(collection.Qualifiers!);
        Empty(innerProperty.Qualifiers!);
    }

    [Fact]
    public void FillOutTemplate_RemovesInternalSemanticIdQualifier_FromNestedSubmodelElementList()
    {
        var innerProperty = new Property(
            idShort: "ListItem",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/item")
            ]);
        var list = new SubmodelElementList(
            AasSubmodelElements.Property,
            idShort: "List",
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/list")
            ],
            value: [innerProperty]);

        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { list };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(list).Returns("http://test/list");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Empty(list.Qualifiers!);
        Empty(innerProperty.Qualifiers!);
    }

    [Fact]
    public void FillOutTemplate_RemovesInternalSemanticIdQualifier_FromNestedEntity()
    {
        var statement = new Property(
            idShort: "Statement",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/statement")
            ]);
        var entity = new Entity(
            EntityType.CoManagedEntity,
            idShort: "Entity",
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/entity")
            ],
            statements: [statement]);

        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { entity };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(entity).Returns("http://test/entity");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Empty(entity.Qualifiers!);
        Empty(statement.Qualifiers!);
    }

    [Fact]
    public void FillOutTemplate_ElementWithNoQualifiers_DoesNotThrow()
    {
        var property = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);
        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { property };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(property).Returns("http://test/prop");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        var result = _sut.FillOutTemplate(submodel, values);

        NotNull(result);
        Null(property.Qualifiers);
    }

    [Fact]
    public void FillOutTemplate_ElementWithOnlyNonInternalQualifiers_PreservesAll()
    {
        var property = new Property(
            idShort: "Prop",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "ExternalReference", valueType: DataTypeDefXsd.String, value: "ZeroToOne"),
                new Qualifier(type: "OtherQualifier", valueType: DataTypeDefXsd.String, value: "SomeValue")
            ]);
        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { property };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(property).Returns("http://test/prop");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Equal(2, property.Qualifiers!.Count);
    }

    [Fact]
    public void FillOutTemplate_DeeplyNestedElements_RemovesInternalSemanticIdAtAllLevels()
    {
        var deepProperty = new Property(
            idShort: "DeepProp",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/deep")
            ]);
        var innerCollection = new SubmodelElementCollection(
            idShort: "InnerCollection",
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/inner")
            ],
            value: [deepProperty]);
        var outerCollection = new SubmodelElementCollection(
            idShort: "OuterCollection",
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/outer")
            ],
            value: [innerCollection]);

        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { outerCollection };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(outerCollection).Returns("http://test/outer");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Empty(outerCollection.Qualifiers!);
        Empty(innerCollection.Qualifiers!);
        Empty(deepProperty.Qualifiers!);
    }

    [Fact]
    public void FillOutTemplate_MultipleElementsWithInternalSemanticId_RemovesFromAll()
    {
        var prop1 = new Property(
            idShort: "Prop1",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/1")
            ]);
        var prop2 = new Property(
            idShort: "Prop2",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/2")
            ]);
        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { prop1, prop2 };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(prop1).Returns("http://test/1");
        _resolver.ExtractSemanticId(prop2).Returns("http://test/2");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Empty(prop1.Qualifiers!);
        Empty(prop2.Qualifiers!);
    }

    [Fact]
    public void FillOutTemplate_TwoQualifiers_RemovesOnlyInternalSemanticId()
    {
        var property = new Property(
            idShort: "Prop",
            valueType: DataTypeDefXsd.String,
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/internal"),
                new Qualifier(type: "ExternalReference", valueType: DataTypeDefXsd.String, value: "ZeroToOne")
            ]);
        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { property };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(property).Returns("http://test/prop");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Single(property.Qualifiers!);
        Equal("ExternalReference", property.Qualifiers[0].Type);
        Equal("ZeroToOne", property.Qualifiers[0].Value);
        DoesNotContain(property.Qualifiers, q => q.Type == "InternalSemanticId");
    }

    [Fact]
    public void FillOutTemplate_ReferenceElementWithTwoQualifiers_RemovesOnlyInternalSemanticId()
    {
        var refElement = new ReferenceElement(
            idShort: "RefElement",
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/ref-internal"),
                new Qualifier(type: "ExternalReference", valueType: DataTypeDefXsd.String, value: "ZeroToOne")
            ],
            value: new Reference(
                ReferenceTypes.ExternalReference,
                [new Key(KeyTypes.GlobalReference, "http://example.com/ref")]));

        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { refElement };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(refElement).Returns("http://test/ref");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Single(refElement.Qualifiers!);
        Equal("ExternalReference", refElement.Qualifiers![0].Type);
        Equal("ZeroToOne", refElement.Qualifiers[0].Value);
        DoesNotContain(refElement.Qualifiers, q => q.Type == "InternalSemanticId");
    }

    [Fact]
    public void FillOutTemplate_RelationshipElementWithTwoQualifiers_RemovesOnlyInternalSemanticId()
    {
        var relationship = new RelationshipElement(
            first: new Reference(
                ReferenceTypes.ExternalReference,
                [new Key(KeyTypes.GlobalReference, "http://example.com/first")]),
            second: new Reference(
                ReferenceTypes.ExternalReference,
                [new Key(KeyTypes.GlobalReference, "http://example.com/second")]),
            idShort: "RelElement",
            qualifiers: [
                new Qualifier(type: "InternalSemanticId", valueType: DataTypeDefXsd.String, value: "http://test/rel-internal"),
                new Qualifier(type: "ExternalReference", valueType: DataTypeDefXsd.String, value: "One")
            ]);

        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { relationship };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(relationship).Returns("http://test/rel");
        _resolver.InternalSemanticIdType.Returns("InternalSemanticId");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Single(relationship.Qualifiers!);
        Equal("ExternalReference", relationship.Qualifiers![0].Type);
        Equal("One", relationship.Qualifiers[0].Value);
        DoesNotContain(relationship.Qualifiers, q => q.Type == "InternalSemanticId");
    }
}
