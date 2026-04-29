using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests;

/// <summary>
///     This validates that the Onion Architecture as described in the SAD is not broken.
///     https://dev.azure.com/mm-products/AAS.TwinEngine/_wiki/wikis/Wiki/163/5-Solution-Strategy
/// </summary>
public class CleanArchitectureTests
{
    private const string BaseNamespace = "AAS.TwinEngine.Plugin.TestPlugin";

    private readonly Architecture _architecture = new ArchLoader().LoadAssemblies(System.Reflection.Assembly.Load(BaseNamespace)).Build();
    private readonly IObjectProvider<IType> _apiLayer = Types().That().ResideInNamespaceMatching($"{BaseNamespace}.Api.*").As("Api");
    private readonly IObjectProvider<IType> _applicationLogicLayer = Types().That().ResideInNamespaceMatching($"{BaseNamespace}.ApplicationLogic.*").As("ApplicationLogic");
    private readonly IObjectProvider<IType> _domainModelLayer = Types().That().ResideInNamespaceMatching($"{BaseNamespace}.DomainModel*").As("DomainModel");
    private readonly IObjectProvider<IType> _infrastructureLayer = Types().That().ResideInNamespaceMatching($"{BaseNamespace}.Infrastructure.*").As("Infrastructure");

    [Fact]
    public void DomainModelShallNotHaveExternalDependencies()
    {
        var forbiddenTypes = new List<IType>();

        forbiddenTypes.AddRange(_infrastructureLayer.GetObjects(_architecture));
        forbiddenTypes.AddRange(_apiLayer.GetObjects(_architecture));
        forbiddenTypes.AddRange(_applicationLogicLayer.GetObjects(_architecture));

        Types().That().Are(_domainModelLayer)
            .Should()
            .NotDependOnAny(Types().That().Are(forbiddenTypes))
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void ApplicationLogicShallNotHaveDependenciesToInfrastructure()
    {
        Types().That().Are(_applicationLogicLayer)
            .Should()
            .NotDependOnAny(Types().That().Are(_infrastructureLayer))
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void ApplicationLogicShallNotHaveDependenciesToApi()
    {
        Types().That().Are(_applicationLogicLayer)
            .Should()
            .NotDependOnAny(Types().That().Are(_apiLayer))
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void InfrastructureShallNotHaveDependenciesToApi()
    {
        Types().That().Are(_infrastructureLayer)
            .Should()
            .NotDependOnAny(Types().That().Are(_apiLayer))
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void ApiShallNotHaveDependenciesToInfrastructure()
    {
        Types().That().Are(_apiLayer)
           .Should()
           .NotDependOnAny(Types().That().Are(_infrastructureLayer))
           .WithoutRequiringPositiveResults()
           .Check(_architecture);
    }

    [Fact]
    public void RepositoryClassesShallBeInCorrectNamespace()
    {
        Classes().That().HaveNameEndingWith("Repository").Should()
            .ResideInNamespaceMatching($"{BaseNamespace}.Infrastructure.Providers*")
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void RepositoryInterfacesShallBeInCorrectNamespace()
    {
        Interfaces().That()
            .HaveNameEndingWith("Repository")
            .And()
            .DoNotHaveFullName($"{BaseNamespace}.Infrastructure.DataAccess.GenericRepository.IMongoDbRepository")
            .Should()
            .ResideInNamespaceMatching($"{BaseNamespace}.ApplicationLogic.*")
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void ServicesShallBeInCorrectNamespace()
    {
        Classes().That().HaveNameEndingWith("Service").Should()
            .ResideInNamespaceMatching($"{BaseNamespace}.ApplicationLogic.Service.*")
            .Check(_architecture);
    }

    [Fact]
    public void ServiceInterfacesShallBeInCorrectNamespace()
    {
        Interfaces().That().HaveNameEndingWith("Service")
            .Should()
            .ResideInNamespaceMatching($"{BaseNamespace}.ApplicationLogic.Service.*")
            .Check(_architecture);
    }

    [Fact]
    public void ControllerShallBeInCorrectNamespace()
    {
        Classes().That().HaveNameEndingWith("Controller").Should()
            .ResideInNamespaceMatching($"{BaseNamespace}.Api.*")
            .Check(_architecture);
    }
}
