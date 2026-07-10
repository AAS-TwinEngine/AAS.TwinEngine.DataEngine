using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRegistry;

public class SubmodelDescriptorServiceTests
{
    private readonly ISubmodelDescriptorProvider _provider = Substitute.For<ISubmodelDescriptorProvider>();
    private readonly IAasRepositoryService _aasRepositoryService = Substitute.For<IAasRepositoryService>();
    private readonly SubmodelDescriptorService _sut;
    private readonly IOptions<GeneralConfig> _options;
    private readonly IOptions<TemplateManagementConfig> _templateManagementOptions;
    private readonly ILogger<SubmodelDescriptorService> _logger = Substitute.For<ILogger<SubmodelDescriptorService>>();
    private readonly ISubmodelTemplateMappingProvider _submodelTemplateMappingProvider = Substitute.For<ISubmodelTemplateMappingProvider>();

    public SubmodelDescriptorServiceTests()
    {
        _options = Options.Create(new GeneralConfig
        {
            DataEngineRepositoryBaseUrl = new Uri("https://www.mm-software.com"),
        });
        _templateManagementOptions = Options.Create(new TemplateManagementConfig
        {
            SubmodelTemplateRegistry = new ServiceInstance
            {
                ConcurrentOperationsLimit = 10
            }
        });
        _sut = new SubmodelDescriptorService(_provider, _submodelTemplateMappingProvider, _aasRepositoryService, _options, _templateManagementOptions, _logger);
    }

    [Fact]
    public async Task GetAllSubmodelDescriptorsAsync_ReturnsPagedDescriptorsDerivedFromShellSubmodelIds()
    {
        var shells = new Shells
        {
            Result =
            [
                CreateShell("ContactInformation"),
                CreateShell("Nameplate")
            ]
        };
        _aasRepositoryService.GetShellsByFiltersAsync(null, null, null, Arg.Any<CancellationToken>())
                           .Returns(shells);
        _submodelTemplateMappingProvider.GetTemplateId("ContactInformation").Returns("ContactInformation");
        _submodelTemplateMappingProvider.GetTemplateId("Nameplate").Returns("Nameplate");
        _provider.GetDataForSubmodelDescriptorByIdAsync("ContactInformation", Arg.Any<CancellationToken>())
                 .Returns(new SubmodelDescriptor { Id = "ContactInformation" });
        _provider.GetDataForSubmodelDescriptorByIdAsync("Nameplate", Arg.Any<CancellationToken>())
                 .Returns(new SubmodelDescriptor { Id = "Nameplate" });

        var result = await _sut.GetAllSubmodelDescriptorsAsync(1, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Result!);
        Assert.Equal("ContactInformation", result.Result![0].Id);
        Assert.NotNull(result.PagingMetaData?.Cursor);
        Assert.StartsWith("https://www.mm-software.com/submodels/", result.Result![0].Endpoints![0].ProtocolInformation!.Href, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllSubmodelDescriptorsAsync_SkipsDescriptorWhenSingleDescriptorLookupFails()
    {
        var shells = new Shells
        {
            Result =
            [
                CreateShell("ValidSubmodelId"),
                CreateShell("MissingSubmodel")
            ]
        };
        _aasRepositoryService.GetShellsByFiltersAsync(null, null, null, Arg.Any<CancellationToken>())
                           .Returns(shells);
        _submodelTemplateMappingProvider.GetTemplateId("ValidSubmodelId").Returns("ValidSubmodelId");
        _submodelTemplateMappingProvider.GetTemplateId("MissingSubmodel").Returns("MissingSubmodel");
        _provider.GetDataForSubmodelDescriptorByIdAsync("ValidSubmodelId", Arg.Any<CancellationToken>())
                 .Returns(new SubmodelDescriptor { Id = "ValidSubmodelId" });
        _provider.GetDataForSubmodelDescriptorByIdAsync("MissingSubmodel", Arg.Any<CancellationToken>())
                 .Throws(new ResourceNotFoundException());

        var result = await _sut.GetAllSubmodelDescriptorsAsync(5, null, CancellationToken.None);

        Assert.Single(result.Result!);
        Assert.Equal("ValidSubmodelId", result.Result![0].Id);
    }

    [Fact]
    public async Task GetAllSubmodelDescriptorsAsync_ThrowsSubmodelDescriptorNotFoundException_WhenAllLookupsFail()
    {
        var shells = new Shells
        {
            Result =
            [
                CreateShell("MissingSubmodel1"),
                CreateShell("MissingSubmodel2")
            ]
        };
        _aasRepositoryService.GetShellsByFiltersAsync(null, null, null, Arg.Any<CancellationToken>())
                           .Returns(shells);
        _submodelTemplateMappingProvider.GetTemplateId("MissingSubmodel1").Returns("MissingSubmodel1");
        _submodelTemplateMappingProvider.GetTemplateId("MissingSubmodel2").Returns("MissingSubmodel2");
        _provider.GetDataForSubmodelDescriptorByIdAsync("MissingSubmodel1", Arg.Any<CancellationToken>())
                 .Throws(new ResourceNotFoundException());
        _provider.GetDataForSubmodelDescriptorByIdAsync("MissingSubmodel2", Arg.Any<CancellationToken>())
                 .Throws(new ResourceNotFoundException());

        await Assert.ThrowsAsync<SubmodelDescriptorNotFoundException>(() =>
            _sut.GetAllSubmodelDescriptorsAsync(5, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllSubmodelDescriptorsAsync_ReturnsEmptyResult_WhenShellsHaveNoSubmodels()
    {
        var shells = new Shells
        {
            Result = []
        };
        _aasRepositoryService.GetShellsByFiltersAsync(null, null, null, Arg.Any<CancellationToken>())
                           .Returns(shells);

        var result = await _sut.GetAllSubmodelDescriptorsAsync(5, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Result!);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_UpdatesHref_WhenProtocolInformationExists()
    {
        const string Id = "ContactInformation";
        var descriptor = new SubmodelDescriptor
        {
            Id = Id,
            Endpoints =
            [
                new EndpointData
                {
                    ProtocolInformation = new ProtocolInformationData()
                    {
                        Href = "oldHref"
                    }
                }
            ]
        };
        _submodelTemplateMappingProvider.GetTemplateId(Id).Returns(Id);
        _provider.GetDataForSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())
                 .Returns(descriptor);

        var result = await _sut.GetSubmodelDescriptorByIdAsync(Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(Id, result.Id);
        Assert.Single(result!.Endpoints!);
        Assert.NotNull(result!.Endpoints![0].ProtocolInformation);
        Assert.StartsWith("https://www.mm-software.com/submodels/", result!.Endpoints[0]!.ProtocolInformation!.Href, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_SetsHref_WhenEndpointsAreNull()
    {
        const string Id = "ContactInformation";
        var descriptor = new SubmodelDescriptor
        {
            Id = Id,
            Endpoints = null
        };
        _submodelTemplateMappingProvider.GetTemplateId(Id).Returns(Id);
        _provider.GetDataForSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())
                 .Returns(descriptor);

        var result = await _sut.GetSubmodelDescriptorByIdAsync(Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Endpoints);
        Assert.Single(result.Endpoints);
        Assert.NotNull(result.Endpoints[0].ProtocolInformation);
        Assert.StartsWith("https://www.mm-software.com/submodels/", result!.Endpoints[0]!.ProtocolInformation!.Href, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_SetsHref_WhenProtocolInformationIsNull()
    {
        const string Id = "ContactInformation";
        var descriptor = new SubmodelDescriptor
        {
            Id = Id,
            Endpoints =
            [
                new EndpointData
                {
                    ProtocolInformation = null
                }
            ]
        };
        _submodelTemplateMappingProvider.GetTemplateId(Id).Returns(Id);
        _provider.GetDataForSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())
                 .Returns(descriptor);

        var result = await _sut.GetSubmodelDescriptorByIdAsync(Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Endpoints);
        Assert.Single(result.Endpoints);
        Assert.NotNull(result.Endpoints[0].ProtocolInformation);
        Assert.StartsWith("https://www.mm-software.com/submodels/", result!.Endpoints[0]!.ProtocolInformation!.Href, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_ThrowsSubmodelDescriptorNotFound_WhenResourceNotFound()
    {
        const string Id = "MissingSubmodel";
        _submodelTemplateMappingProvider.GetTemplateId(Id).Returns(Id);
        _provider.GetDataForSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())
                 .Throws(new ResourceNotFoundException());

        var ex = await Assert.ThrowsAsync<SubmodelDescriptorNotFoundException>(() =>
            _sut.GetSubmodelDescriptorByIdAsync(Id, CancellationToken.None));

        Assert.Contains(Id, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_ThrowsInternalDataProcessing_WhenResponseParsingFails()
    {
        const string Id = "ParsingError";
        _submodelTemplateMappingProvider.GetTemplateId(Id).Returns(Id);
        _provider.GetDataForSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())
                 .Throws(new ResponseParsingException());

        var ex = await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            _sut.GetSubmodelDescriptorByIdAsync(Id, CancellationToken.None));

        Assert.IsType<InternalDataProcessingException>(ex);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_ThrowsRegistryNotAvailable_WhenTimeoutOccurs()
    {
        const string Id = "TimeoutSubmodel";
        _submodelTemplateMappingProvider.GetTemplateId(Id).Returns(Id);
        _provider.GetDataForSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())
                 .Throws(new RequestTimeoutException());

        var ex = await Assert.ThrowsAsync<RegistryNotAvailableException>(() =>
            _sut.GetSubmodelDescriptorByIdAsync(Id, CancellationToken.None));

        Assert.IsType<RegistryNotAvailableException>(ex);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_ThrowsRegistryNotAvailable_WhenServiceUnavailable()
    {
        const string Id = "UnavailableSubmodel";
        _submodelTemplateMappingProvider.GetTemplateId(Id).Returns(Id);
        _provider.GetDataForSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())
                 .Throws(new RegistryNotAvailableException());

        var ex = await Assert.ThrowsAsync<RegistryNotAvailableException>(() =>
            _sut.GetSubmodelDescriptorByIdAsync(Id, CancellationToken.None));

        Assert.IsType<RegistryNotAvailableException>(ex);
    }

    private static AssetAdministrationShell CreateShell(string submodelId)
    {
        return new AssetAdministrationShell(
            id: "shell-id",
            assetInformation: new AssetInformation(assetKind: AssetKind.Instance, globalAssetId: null),
            submodels:
            [
                new Reference(
                    type: ReferenceTypes.ModelReference,
                    keys: [new Key(KeyTypes.Submodel, submodelId)],
                    referredSemanticId: null)
            ]);
    }
}
