using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;
using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.UnitTests.ApplicationLogic.Services.Crud;

public class CrudDecisionMakerTests
{
    private readonly CrudDecisionMaker _sut = new();

    [Fact]
    public void Decide_WhenBothEmpty_ReturnsEmptyDecisions()
    {
        // Act
        var decisions = _sut.Decide(
            EntityKind.Submodel,
            Array.Empty<SourceEntity>(),
            Array.Empty<ExportedEntity>());

        // Assert
        Assert.Empty(decisions);
    }

    [Fact]
    public void Decide_WhenAllSourceEntitiesAreNew_ReturnsCreateDecisions()
    {
        // Arrange
        var source1 = new SourceEntity("id1", "{\"id\":\"id1\"}");
        var source2 = new SourceEntity("id2", "{\"id\":\"id2\"}");
        var sources = new[] { source1, source2 };
        var state = Array.Empty<ExportedEntity>();

        // Act
        var decisions = _sut.Decide(EntityKind.ConceptDescription, sources, state);

        // Assert
        Assert.Equal(2, decisions.Count);
        Assert.All(decisions, d =>
        {
            Assert.Equal(ExportOperation.Create, d.Operation);
            Assert.Equal(EntityKind.ConceptDescription, d.Kind);
            Assert.NotNull(d.SourceEntity);
        });
        Assert.Equal("id1", decisions[0].Identifier);
        Assert.Same(source1, decisions[0].SourceEntity);
        Assert.Equal("id2", decisions[1].Identifier);
        Assert.Same(source2, decisions[1].SourceEntity);
    }

    [Fact]
    public void Decide_WhenAllSourceEntitiesExistInManagedState_ReturnsUpdateDecisions()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var source1 = new SourceEntity("id1", "{\"id\":\"id1\"}");
        var source2 = new SourceEntity("id2", "{\"id\":\"id2\"}");
        var sources = new[] { source1, source2 };
        var state = new[]
        {
            new ExportedEntity(EntityKind.Shell, "id1", now, now),
            new ExportedEntity(EntityKind.Shell, "id2", now, now)
        };

        // Act
        var decisions = _sut.Decide(EntityKind.Shell, sources, state);

        // Assert
        Assert.Equal(2, decisions.Count);
        Assert.All(decisions, d =>
        {
            Assert.Equal(ExportOperation.Update, d.Operation);
            Assert.Equal(EntityKind.Shell, d.Kind);
            Assert.NotNull(d.SourceEntity);
        });
        Assert.Equal("id1", decisions[0].Identifier);
        Assert.Same(source1, decisions[0].SourceEntity);
        Assert.Equal("id2", decisions[1].Identifier);
        Assert.Same(source2, decisions[1].SourceEntity);
    }

    [Fact]
    public void Decide_WhenManagedStateHasEntitiesNotInSource_ReturnsDeleteDecisions()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sources = Array.Empty<SourceEntity>();
        var state = new[]
        {
            new ExportedEntity(EntityKind.SubmodelDescriptor, "id_old1", now, now),
            new ExportedEntity(EntityKind.SubmodelDescriptor, "id_old2", now, now)
        };

        // Act
        var decisions = _sut.Decide(EntityKind.SubmodelDescriptor, sources, state);

        // Assert
        Assert.Equal(2, decisions.Count);
        Assert.All(decisions, d =>
        {
            Assert.Equal(ExportOperation.Delete, d.Operation);
            Assert.Equal(EntityKind.SubmodelDescriptor, d.Kind);
            Assert.Null(d.SourceEntity);
        });
        Assert.Equal("id_old1", decisions[0].Identifier);
        Assert.Equal("id_old2", decisions[1].Identifier);
    }

    [Fact]
    public void Decide_WhenMixedEntities_ReturnsCreateUpdateDeleteDecisionsInExpectedOrder()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sourceNew = new SourceEntity("id_new", "{\"id\":\"id_new\"}");
        var sourceExisting = new SourceEntity("id_existing", "{\"id\":\"id_existing\"}");
        var sources = new[] { sourceNew, sourceExisting };
        var state = new[]
        {
            new ExportedEntity(EntityKind.ShellDescriptor, "id_existing", now, now),
            new ExportedEntity(EntityKind.ShellDescriptor, "id_deleted", now, now)
        };

        // Act
        var decisions = _sut.Decide(EntityKind.ShellDescriptor, sources, state);

        // Assert
        Assert.Equal(3, decisions.Count);

        var create = decisions.Single(d => d.Operation == ExportOperation.Create);
        Assert.Equal("id_new", create.Identifier);
        Assert.Same(sourceNew, create.SourceEntity);
        Assert.Equal(EntityKind.ShellDescriptor, create.Kind);

        var update = decisions.Single(d => d.Operation == ExportOperation.Update);
        Assert.Equal("id_existing", update.Identifier);
        Assert.Same(sourceExisting, update.SourceEntity);
        Assert.Equal(EntityKind.ShellDescriptor, update.Kind);

        var delete = decisions.Single(d => d.Operation == ExportOperation.Delete);
        Assert.Equal("id_deleted", delete.Identifier);
        Assert.Null(delete.SourceEntity);
        Assert.Equal(EntityKind.ShellDescriptor, delete.Kind);
    }

    [Fact]
    public void Decide_StringComparisonIsOrdinal()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var source = new SourceEntity("Id1", "{\"id\":\"Id1\"}");
        var state = new[]
        {
            new ExportedEntity(EntityKind.Shell, "id1", now, now)
        };

        // Act
        var decisions = _sut.Decide(EntityKind.Shell, new[] { source }, state);

        // Assert - "Id1" != "id1" with StringComparer.Ordinal
        Assert.Equal(2, decisions.Count);
        Assert.Equal(ExportOperation.Create, decisions[0].Operation);
        Assert.Equal("Id1", decisions[0].Identifier);
        Assert.Equal(ExportOperation.Delete, decisions[1].Operation);
        Assert.Equal("id1", decisions[1].Identifier);
    }
}
