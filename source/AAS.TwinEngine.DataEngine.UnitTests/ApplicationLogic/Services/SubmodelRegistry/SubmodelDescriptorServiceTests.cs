using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRegistry;

public class SubmodelDescriptorServiceTests
{
    private readonly ISubmodelDescriptorProvider _provider = Substitute.For<ISubmodelDescriptorProvider>();
    private readonly SubmodelDescriptorService _sut;
    private readonly IOptions<GeneralConfig> _options;
    private readonly ILogger<SubmodelDescriptorService> _logger = Substitute.For<ILogger<SubmodelDescriptorService>>();
    private readonly ISubmodelTemplateMappingProvider _submodelTemplateMappingProvider = Substitute.For<ISubmodelTemplateMappingProvider>();

    public SubmodelDescriptorServiceTests()
    {
        _options = Options.Create(new GeneralConfig
        {
            DataEngineRepositoryBaseUrl = new Uri("https://www.mm-software.com"),
        });
        _sut = new SubmodelDescriptorService(_provider, _submodelTemplateMappingProvider, _options, _logger);
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
}
