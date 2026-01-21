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

    private readonly Architecture _architecture;
    private readonly IObjectProvider<IType> _apiLayer;
    private readonly IObjectProvider<IType> _applicationLogicLayer;
    private readonly IObjectProvider<IType> _domainModelLayer;
    private readonly IObjectProvider<IType> _infrastructureLayer;

    public CleanArchitectureTests()
    {
        _architecture = new ArchLoader().LoadAssemblies(System.Reflection.Assembly.Load(BaseNamespace)).Build();

        _apiLayer = Types().That().ResideInNamespace($"{BaseNamespace}.Api.*", true).As("Api");
        _applicationLogicLayer = Types().That().ResideInNamespace($"{BaseNamespace}.ApplicationLogic.*", true).As("ApplicationLogic");
        _domainModelLayer = Types().That().ResideInNamespace($"{BaseNamespace}.DomainModel*", true).As("DomainModel");
        _infrastructureLayer = Types().That().ResideInNamespace($"{BaseNamespace}.Infrastructure.*", true).As("Infrastructure");
    }

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
            .Check(_architecture);
    }

    [Fact]
    public void ApplicationLogicShallNotHaveDependenciesToInfrastructure()
    {
        Types().That().Are(_applicationLogicLayer)
            .Should()
            .NotDependOnAny(Types().That().Are(_infrastructureLayer))
            .Check(_architecture);
    }

    [Fact]
    public void ApplicationLogicShallNotHaveDependenciesToApi()
    {
        Types().That().Are(_applicationLogicLayer)
            .Should()
            .NotDependOnAny(Types().That().Are(_apiLayer))
            .Check(_architecture);
    }

    [Fact]
    public void InfrastructureShallNotHaveDependenciesToApi()
    {
        Types().That().Are(_infrastructureLayer)
            .Should()
            .NotDependOnAny(Types().That().Are(_apiLayer))
            .Check(_architecture);
    }

    [Fact]
    public void ApiShallNotHaveDependenciesToInfrastructure()
    {
        Types().That().Are(_apiLayer)
           .Should()
           .NotDependOnAny(Types().That().Are(_infrastructureLayer))
           .Check(_architecture);
    }

    [Fact]
    public void RepositoryClassesShallBeInCorrectNamespace()
    {
        Classes().That().HaveNameEndingWith("Repository").Should()
            .ResideInNamespace($"{BaseNamespace}.Infrastructure.DataAccess*", true)
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
            .ResideInNamespace($"{BaseNamespace}.ApplicationLogic.*", true)
            .WithoutRequiringPositiveResults()
            .Check(_architecture);
    }

    [Fact]
    public void ServicesShallBeInCorrectNamespace()
    {
        Classes().That().HaveNameEndingWith("Service").Should()
            .ResideInNamespace($"{BaseNamespace}.ApplicationLogic.Service.*", true)
            .Check(_architecture);
    }

    [Fact]
    public void ServiceInterfacesShallBeInCorrectNamespace()
    {
        Interfaces().That().HaveNameEndingWith("Service")
            .Should()
            .ResideInNamespace($"{BaseNamespace}.ApplicationLogic.Service.*", true)
            .Check(_architecture);
    }

    [Fact]
    public void ControllerShallBeInCorrectNamespace()
    {
        Classes().That().HaveNameEndingWith("Controller").Should()
            .ResideInNamespace($"{BaseNamespace}.Api.*", true)
            .Check(_architecture);
    }
}