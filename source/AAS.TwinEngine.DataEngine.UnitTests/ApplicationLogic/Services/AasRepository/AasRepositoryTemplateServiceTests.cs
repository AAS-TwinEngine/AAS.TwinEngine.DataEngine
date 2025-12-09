using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;

using AasCore.Aas3_0;

using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.AasRepository;

public class AasRepositoryTemplateServiceTests
{
    private readonly ITemplateProvider _templateProvider = Substitute.For<ITemplateProvider>();
    private readonly IShellTemplateMappingProvider _shellTemplateMappingProvider = Substitute.For<IShellTemplateMappingProvider>();
    private readonly IOptions<AasEnvironmentConfig> _config = Substitute.For<IOptions<AasEnvironmentConfig>>();
    private readonly Uri _mockCustomerDomainUrl = new("https://mm-software-test.com");

    private readonly AasRepositoryTemplateService _sut;
    private const string AasIdentifier = "testAas";
    private const string TemplateId = "template-001";

    public AasRepositoryTemplateServiceTests()
    {
        _config.Value.Returns(new AasEnvironmentConfig
        {
            CustomerDomainUrl = _mockCustomerDomainUrl
        });

        _sut = new AasRepositoryTemplateService(
            _templateProvider,
            _shellTemplateMappingProvider,
            _config
        );
    }

    [Fact]
    public async Task GetShellTemplateAsync_ShouldUpdateSubmodelKeyValues()
    {
        // Arrange
        const string ProductId = "product-456";

        var shell = CreateShell("urn:uuid:submodel-123");

        _shellTemplateMappingProvider.GetTemplateId(AasIdentifier).Returns(TemplateId);
        _shellTemplateMappingProvider.GetProductIdFromRule(AasIdentifier).Returns(ProductId);
        _templateProvider.GetShellTemplateAsync(TemplateId, Arg.Any<CancellationToken>()).Returns(shell);

        // Act
        var result = await _sut.GetShellTemplateAsync(AasIdentifier, CancellationToken.None);

        // Assert
        var key = result.Submodels!.First().Keys.First();
        Assert.StartsWith($"{_mockCustomerDomainUrl}submodel/{ProductId}/", key.Value);
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_ShouldReturnExpectedAssetInformation()
    {
        // Arrange
        var expectedAssetInfo = CreateAssetInformation();

        _shellTemplateMappingProvider.GetTemplateId(AasIdentifier).Returns(TemplateId);
        _templateProvider.GetAssetInformationTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(expectedAssetInfo);

        // Act
        var result = await _sut.GetAssetInformationTemplateAsync(AasIdentifier, CancellationToken.None);

        // Assert
        Assert.Equal(expectedAssetInfo, result);
    }

    [Fact]
    public async Task GetShellTemplateAsync_ShouldThrowTemplateNotFound_WhenResourceNotFound()
    {
        _shellTemplateMappingProvider.GetTemplateId(AasIdentifier).Returns(TemplateId);
        _templateProvider.GetShellTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        await Assert.ThrowsAsync<TemplateNotFoundException>(
            () => _sut.GetShellTemplateAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellTemplateAsync_ShouldThrowInternalDataProcessing_WhenResponseParsingFails()
    {
        _shellTemplateMappingProvider.GetTemplateId(AasIdentifier).Returns(TemplateId);
        _templateProvider.GetShellTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Throws(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(
            () => _sut.GetShellTemplateAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellTemplateAsync_ShouldThrowRepositoryNotAvailable_WhenTimeoutOccurs()
    {
        _shellTemplateMappingProvider.GetTemplateId(AasIdentifier).Returns(TemplateId);
        _templateProvider.GetShellTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Throws(new RequestTimeoutException());

        await Assert.ThrowsAsync<RepositoryNotAvailableException>(
            () => _sut.GetShellTemplateAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ShouldThrowInternalDataProcessing_WhenUnexpectedExceptionOccurs()
    {
        _templateProvider.GetSubmodelRefByIdAsync(TemplateId, Arg.Any<CancellationToken>())
            .Throws(new Exception("unexpected failure"));

        await Assert.ThrowsAsync<InternalDataProcessingException>(
            () => _sut.GetSubmodelRefByIdAsync(AasIdentifier, CancellationToken.None));
    }

    private static AssetAdministrationShell CreateShell(string keyValue)
    {
        IReference submodelRef = new Reference(
            type: ReferenceTypes.ModelReference,
            keys:
            [
            new Key(KeyTypes.Submodel, keyValue)
            ],
            referredSemanticId: null
        );

        return new AssetAdministrationShell(
            id: "urn:uuid:123e4567-e89b-12d3-a456-426614174000",
            assetInformation: new AssetInformation(
                assetKind: AssetKind.Instance,
                globalAssetId: null
            ),
            idShort: "exampleAAS",
            category: "exampleCategory",
            displayName:
            [
                new LangStringNameType(language: "en", text: "Example AAS")
            ],
            description:
            [
                new LangStringTextType(language: "en", text: "This is a sample Asset Administration Shell")
            ],
            submodels: [submodelRef]
        );
    }

    private static AssetInformation CreateAssetInformation()
    {
        return new AssetInformation(
            assetKind: AssetKind.Instance,
            globalAssetId: "urn:uuid:global-asset-001",
            specificAssetIds: null,
            assetType: "http://example.com/assetType/ModelX",
            defaultThumbnail: null
        );
    }
}

