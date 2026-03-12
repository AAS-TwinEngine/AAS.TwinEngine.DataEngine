using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class EntityHandlerTests
{
    private readonly EntityHandler _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly ILogger<EntityHandler> _logger;

    public EntityHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _logger = Substitute.For<ILogger<EntityHandler>>();
        _sut = new EntityHandler(_resolver, _logger);
    }

    [Fact]
    public void CanHandle_Entity_ReturnsTrue()
    {
        var entity = new Entity(idShort: "Test", entityType: EntityType.SelfManagedEntity);

        True(_sut.CanHandle(entity));
    }

    [Fact]
    public void CanHandle_NonEntity_ReturnsFalse()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        False(_sut.CanHandle(property));
    }

    [Fact]
    public void Extract_SelfManagedEntity_ReturnsBranchWithGlobalAssetIdAndSpecificAssetIds()
    {
        var specificAssetId = new SpecificAssetId(name: "Manufacturer", value: "Corp")
        {
            SemanticId = new Reference(ReferenceTypes.ModelReference,
                [new Key(KeyTypes.ConceptDescription, "https://example.com/cd/manufacturer")])
        };

        var entity = new Entity(
            idShort: "MyEntity",
            entityType: EntityType.SelfManagedEntity,
            globalAssetId: "",
            specificAssetIds: [specificAssetId],
            statements: [new Property(idShort: "Stmt", valueType: DataTypeDefXsd.String)]
        );

        _resolver.ResolveElementSemanticId(entity, "MyEntity").Returns("http://test/entity");
        _resolver.GetCardinality(entity).Returns(Cardinality.ZeroToMany);
        _resolver.GetSemanticId(specificAssetId).Returns("https://example.com/cd/manufacturer");

        var stmtNode = new SemanticLeafNode("http://test/stmt", "", DataType.String, Cardinality.One);

        var result = _sut.Extract(entity, _ => stmtNode);

        var branch = IsType<SemanticBranchNode>(result);
        Equal("http://test/entity", branch.SemanticId);
        Equal(3, branch.Children.Count);
        var globalAssetLeaf = IsType<SemanticLeafNode>(branch.Children[0]);
        Equal("http://test/entity" + SemanticIdResolver.EntityGlobalAssetIdPostFix, globalAssetLeaf.SemanticId);
        var specificLeaf = IsType<SemanticLeafNode>(branch.Children[1]);
        Equal("https://example.com/cd/manufacturer", specificLeaf.SemanticId);
    }

    [Fact]
    public void Extract_CoManagedEntity_DoesNotAddGlobalAssetId()
    {
        var entity = new Entity(
            idShort: "MyEntity",
            entityType: EntityType.CoManagedEntity,
            statements: [new Property(idShort: "Stmt", valueType: DataTypeDefXsd.String)]
        );

        _resolver.ResolveElementSemanticId(entity, "MyEntity").Returns("http://test/entity");
        _resolver.GetCardinality(entity).Returns(Cardinality.One);

        var stmtNode = new SemanticLeafNode("http://test/stmt", "", DataType.String, Cardinality.One);

        var result = _sut.Extract(entity, _ => stmtNode);

        var branch = IsType<SemanticBranchNode>(result);
        Single(branch.Children);
    }

    [Fact]
    public void Extract_EntityWithNoStatements_LogsWarning()
    {
        var entity = new Entity(
            idShort: "MyEntity",
            entityType: EntityType.CoManagedEntity,
            statements: null
        );

        _resolver.ResolveElementSemanticId(entity, "MyEntity").Returns("http://test/entity");
        _resolver.GetCardinality(entity).Returns(Cardinality.One);

        _sut.Extract(entity, _ => null);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("No elements defined in Entity MyEntity")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }

    [Fact]
    public void FillOut_SelfManagedEntity_SetsGlobalAssetIdAndSpecificAssetIds()
    {
        var specificAssetId = new SpecificAssetId(name: "Manufacturer", value: "")
        {
            SemanticId = new Reference(ReferenceTypes.ModelReference,
                [new Key(KeyTypes.ConceptDescription, "https://example.com/cd/manufacturer")])
        };

        var entity = new Entity(
            idShort: "MyEntity",
            entityType: EntityType.SelfManagedEntity,
            globalAssetId: "",
            specificAssetIds: [specificAssetId],
            statements: [new Property(idShort: "Stmt", valueType: DataTypeDefXsd.String)]
        );

        _resolver.ResolveElementSemanticId(entity, "MyEntity").Returns("http://test/entity");
        _resolver.GetSemanticId(specificAssetId).Returns("https://example.com/cd/manufacturer");

        var valueNode = new SemanticBranchNode("http://test/entity", Cardinality.One);
        valueNode.AddChild(new SemanticLeafNode("http://test/entity_globalAssetId", "urn:uuid:12345", DataType.String, Cardinality.One));
        valueNode.AddChild(new SemanticLeafNode("https://example.com/cd/manufacturer", "NewCorp", DataType.String, Cardinality.One));

        _sut.FillOut(entity, valueNode, (_, _, _) => { });

        Equal("urn:uuid:12345", entity.GlobalAssetId);
        Equal("NewCorp", specificAssetId.Value);
    }

    [Fact]
    public void FillOut_WithStatements_DelegatesToFillOutChildren()
    {
        var stmt = new Property(idShort: "Stmt", valueType: DataTypeDefXsd.String);
        var entity = new Entity(
            idShort: "MyEntity",
            entityType: EntityType.CoManagedEntity,
            statements: [stmt]
        );
        var values = new SemanticBranchNode("http://test/entity", Cardinality.One);
        var fillOutCalled = false;

        _sut.FillOut(entity, values, (elements, node, updateIdShort) =>
        {
            fillOutCalled = true;
            True(updateIdShort);
        });

        True(fillOutCalled);
    }

    [Fact]
    public void FillOut_EntityWithNullStatements_DoesNotCallFillOutChildren()
    {
        var entity = new Entity(
            idShort: "MyEntity",
            entityType: EntityType.CoManagedEntity,
            statements: null
        );
        var values = new SemanticBranchNode("http://test/entity", Cardinality.One);

        _sut.FillOut(entity, values, (_, _, _) => Fail("Should not be called"));
    }
}
