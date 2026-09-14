using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Http.Clients;

public class EndpointResolverTests
{
    [Theory]
    [InlineData(EntityKind.ConceptDescription, HttpClientNames.SourceConceptDescriptions)]
    [InlineData(EntityKind.Submodel, HttpClientNames.SourceSubmodels)]
    [InlineData(EntityKind.SubmodelDescriptor, HttpClientNames.SourceSubmodelDescriptors)]
    [InlineData(EntityKind.Shell, HttpClientNames.SourceShells)]
    [InlineData(EntityKind.ShellDescriptor, HttpClientNames.SourceShellDescriptors)]
    public void SourceClientName_MapsCorrectly(EntityKind kind, string expectedClientName)
    {
        var result = EndpointResolver.SourceClientName(kind);
        Assert.Equal(expectedClientName, result);
    }

    [Fact]
    public void SourceClientName_WhenUnknownKind_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EndpointResolver.SourceClientName((EntityKind)999));
    }

    [Theory]
    [InlineData(EntityKind.ConceptDescription, HttpClientNames.TargetConceptDescriptions)]
    [InlineData(EntityKind.Submodel, HttpClientNames.TargetSubmodels)]
    [InlineData(EntityKind.SubmodelDescriptor, HttpClientNames.TargetSubmodelDescriptors)]
    [InlineData(EntityKind.Shell, HttpClientNames.TargetShells)]
    [InlineData(EntityKind.ShellDescriptor, HttpClientNames.TargetShellDescriptors)]
    public void TargetClientName_MapsCorrectly(EntityKind kind, string expectedClientName)
    {
        var result = EndpointResolver.TargetClientName(kind);
        Assert.Equal(expectedClientName, result);
    }

    [Fact]
    public void TargetClientName_WhenUnknownKind_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EndpointResolver.TargetClientName((EntityKind)999));
    }

    [Theory]
    [InlineData(EntityKind.ConceptDescription)]
    [InlineData(EntityKind.Submodel)]
    [InlineData(EntityKind.SubmodelDescriptor)]
    [InlineData(EntityKind.Shell)]
    [InlineData(EntityKind.ShellDescriptor)]
    public void SourceEndpoint_MapsCorrectly(EntityKind kind)
    {
        var sources = new SourceEndpointsConfig();
        var endpoint = EndpointResolver.SourceEndpoint(kind, sources);
        Assert.NotNull(endpoint);

        var expectedEndpoint = kind switch
        {
            EntityKind.ConceptDescription => sources.ConceptDescriptions,
            EntityKind.Submodel => sources.Submodels,
            EntityKind.SubmodelDescriptor => sources.SubmodelDescriptors,
            EntityKind.Shell => sources.Shells,
            EntityKind.ShellDescriptor => sources.ShellDescriptors,
            _ => null
        };
        Assert.Same(expectedEndpoint, endpoint);
    }

    [Fact]
    public void SourceEndpoint_WhenUnknownKind_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EndpointResolver.SourceEndpoint((EntityKind)999, new SourceEndpointsConfig()));
    }

    [Theory]
    [InlineData(EntityKind.ConceptDescription)]
    [InlineData(EntityKind.Submodel)]
    [InlineData(EntityKind.SubmodelDescriptor)]
    [InlineData(EntityKind.Shell)]
    [InlineData(EntityKind.ShellDescriptor)]
    public void TargetEndpoint_MapsCorrectly(EntityKind kind)
    {
        var targets = new TargetEndpointsConfig();
        var endpoint = EndpointResolver.TargetEndpoint(kind, targets);
        Assert.NotNull(endpoint);

        var expectedEndpoint = kind switch
        {
            EntityKind.ConceptDescription => targets.ConceptDescriptions,
            EntityKind.Submodel => targets.Submodels,
            EntityKind.SubmodelDescriptor => targets.SubmodelDescriptors,
            EntityKind.Shell => targets.Shells,
            EntityKind.ShellDescriptor => targets.ShellDescriptors,
            _ => null
        };
        Assert.Same(expectedEndpoint, endpoint);
    }

    [Fact]
    public void TargetEndpoint_WhenUnknownKind_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EndpointResolver.TargetEndpoint((EntityKind)999, new TargetEndpointsConfig()));
    }
}
