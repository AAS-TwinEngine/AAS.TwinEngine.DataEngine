using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.UnitTests.DomainModel;

public class EntityKindRelationsTests
{
    [Theory]
    [InlineData(EntityKind.ShellDescriptor, EntityKind.Shell)]
    [InlineData(EntityKind.SubmodelDescriptor, EntityKind.Submodel)]
    public void DescribedEntityKind_ForDescriptorKinds_ReturnsDescribedKind(EntityKind descriptor, EntityKind expected)
    {
        Assert.Equal(expected, descriptor.DescribedEntityKind());
    }

    [Theory]
    [InlineData(EntityKind.Shell)]
    [InlineData(EntityKind.Submodel)]
    [InlineData(EntityKind.ConceptDescription)]
    public void DescribedEntityKind_ForNonDescriptorKinds_ReturnsNull(EntityKind kind)
    {
        Assert.Null(kind.DescribedEntityKind());
    }
}
