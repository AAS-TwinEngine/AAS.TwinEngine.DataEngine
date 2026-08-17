using AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.AasRepository.Handler;

public class AasRepositoryHandlerTests
{
    private readonly IAasRepositoryService _aasRepositoryService = Substitute.For<IAasRepositoryService>();
    private readonly ILogger<AasRepositoryHandler> _logger = Substitute.For<ILogger<AasRepositoryHandler>>();
    private readonly AasRepositoryHandler _sut;

    public AasRepositoryHandlerTests() => _sut = new AasRepositoryHandler(_logger, _aasRepositoryService);

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithNullAssetIds_ReturnsAllShells()
    {
        _ = _aasRepositoryService.GetShellsByFiltersAsync(Arg.Any<ShellSearchFilter?>(), null, null, Arg.Any<CancellationToken>())
            .Returns(new Shells { PagingMetaData = new PagingMetaData { Cursor = null }, Result = [] });
        var request = new GetShellsByAssetIdsRequest(null, null, null, null);

        var result = await _sut.GetShellsByAssetIdsAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        await _aasRepositoryService.Received().GetShellsByFiltersAsync(Arg.Is<ShellSearchFilter>(f => f.SpecificAssetIds == null && f.IdShort == null), null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithEmptyAssetIds_ReturnsAllShells()
    {
        _ = _aasRepositoryService.GetShellsByFiltersAsync(Arg.Any<ShellSearchFilter?>(), null, null, Arg.Any<CancellationToken>())
            .Returns(new Shells { PagingMetaData = new PagingMetaData { Cursor = null }, Result = [] });
        var request = new GetShellsByAssetIdsRequest([], null, null, null);

        var result = await _sut.GetShellsByAssetIdsAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        await _aasRepositoryService.Received().GetShellsByFiltersAsync(Arg.Is<ShellSearchFilter>(f => f.SpecificAssetIds == null && f.IdShort == null), null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithInvalidBase64Url_ThrowsInvalidUserInputException()
    {
        var assetIds = new[] { "not-valid-base64!!!" };
        var request = new GetShellsByAssetIdsRequest(assetIds, null, null, null);

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithValidInput_ReturnsShells()
    {
        var json = """{"name":"SerialNumber","value":"SN-4711"}""";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded };
        var request = new GetShellsByAssetIdsRequest(assetIds, null, null, null);
        var shell = new AssetAdministrationShell(
            "urn:example:aas:001",
            new AssetInformation(AssetKind.Instance));

        _ = _aasRepositoryService.GetShellsByFiltersAsync(
            Arg.Any<ShellSearchFilter?>(), null, null, Arg.Any<CancellationToken>())
            .Returns(new Shells { PagingMetaData = new PagingMetaData { Cursor = null }, Result = [shell] });

        var result = await _sut.GetShellsByAssetIdsAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Result!);
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithNegativeLimit_ThrowsInvalidUserInputException()
    {
        var json = """{"name":"SerialNumber","value":"SN-4711"}""";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded };

        var request = new GetShellsByAssetIdsRequest(assetIds, null, -1, null);

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithIdShort_PassesIdShortToService()
    {
        const string idShort = "test-idshort";
        _ = _aasRepositoryService.GetShellsByFiltersAsync(Arg.Any<ShellSearchFilter?>(), null, null, Arg.Any<CancellationToken>())
            .Returns(new Shells { PagingMetaData = new PagingMetaData { Cursor = null }, Result = [] });
        var request = new GetShellsByAssetIdsRequest(null, idShort, null, null);

        var result = await _sut.GetShellsByAssetIdsAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        await _aasRepositoryService.Received().GetShellsByFiltersAsync(Arg.Is<ShellSearchFilter>(f => f.SpecificAssetIds == null && f.IdShort == idShort), null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellByIdAsync_ReturnShell_WhenExists()
    {
        const string Id = "AasIdentifier";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetShellRequest(encodedId);
        var shell = new AssetAdministrationShell(
            id: "AasIdentifier",
            assetInformation: new AssetInformation(AssetKind.Instance, null)
            );
        _aasRepositoryService.GetShellByIdAsync(Id, Arg.Any<CancellationToken>()).Returns(shell);

        var result = await _sut.GetShellByIdAsync(request, CancellationToken.None);

        Assert.IsType<AssetAdministrationShell>(result);
        await _aasRepositoryService.Received().GetShellByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellByIdAsync_ShellIsNull_ThrowsTemplateNotFoundException()
    {
        const string Id = "AasIdentifier";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetShellRequest(encodedId);
        _aasRepositoryService.GetShellByIdAsync(Id, Arg.Any<CancellationToken>())!.Returns((AssetAdministrationShell)null!);

        await Assert.ThrowsAsync<TemplateNotFoundException>(() => _sut.GetShellByIdAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ReturnShell_WhenExists()
    {
        const string Id = "AasIdentifier";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetAssetInformationRequest(encodedId);
        var assetInformation = CreateAssetInformation();
        _aasRepositoryService.GetAssetInformationByIdAsync(Id, Arg.Any<CancellationToken>()).Returns(assetInformation);

        var result = await _sut.GetAssetInformationByIdAsync(request, CancellationToken.None);

        Assert.IsType<AssetInformation>(result);
        await _aasRepositoryService.Received().GetAssetInformationByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAssetInformationByIdAsyncAssetInformationIsNull_ThrowsTemplateNotFoundException()
    {
        const string Id = "AasIdentifier";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetAssetInformationRequest(encodedId);
        _aasRepositoryService.GetAssetInformationByIdAsync(Id, Arg.Any<CancellationToken>())!.Returns((AssetInformation)null!);

        await Assert.ThrowsAsync<TemplateNotFoundException>(() => _sut.GetAssetInformationByIdAsync(request, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task InvalidAasIdentifier_ThrowsInvalidUserInputException(string aasIdentifier)
    {
        var encodedId = aasIdentifier.EncodeBase64Url();
        var request = new GetShellRequest(encodedId);

        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                _sut.GetShellByIdAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ReturnsSubmodelRefDto_WhenExists()
    {
        const string Id = "ShellIdentifier";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetSubmodelRefRequest(encodedId, 5, null);
        var expectedDto = new SubmodelRefDto
        {
            PagingMetaData = new PagingMetaDataDto() { Cursor = "" },
            Result =
            [
                new Reference
                (
                 ReferenceTypes.ModelReference,
                 [
                     new Key
                     (
                         KeyTypes.Submodel,
                         "urn:uuid:submodel-123"
                     )],
                 null
                )
            ]
        };
        var domainModel = new SubmodelRef
        {
            PagingMetaData = new PagingMetaData() { Cursor = "" },
            Result =
            [
                new Reference
                (
                 ReferenceTypes.ModelReference,
                 [
                     new Key
                     (
                         KeyTypes.Submodel,
                         "urn:uuid:submodel-123"
                     )],
                 null
                )
            ],
        };
        _aasRepositoryService.GetSubmodelRefByIdAsync(Id, 5, null, Arg.Any<CancellationToken>()).Returns(domainModel);

        var result = await _sut.GetSubmodelRefByIdAsync(request, CancellationToken.None);

        Assert.True(result.TryGetProperty("result", out var resultArray));
        Assert.Equal(domainModel.Result.Count, resultArray.GetArrayLength());
        var firstRef = resultArray[0];
        Assert.True(firstRef.TryGetProperty("keys", out var keysArray));
        Assert.Equal(domainModel.Result.FirstOrDefault()!.Keys.Count, keysArray.GetArrayLength());
        var firstKey = keysArray[0];
        Assert.Equal(domainModel.Result.FirstOrDefault()!.Keys.FirstOrDefault()!.Value, firstKey.GetProperty("value").GetString());
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_SubmodelRefIsNull_ThrowsTemplateNotFoundException()
    {
        const string Id = "ShellIdentifier";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetSubmodelRefRequest(encodedId, 5, null);
        _aasRepositoryService.GetSubmodelRefByIdAsync(Id, 5, null, Arg.Any<CancellationToken>())!.Returns((SubmodelRef)null!);

        await Assert.ThrowsAsync<TemplateNotFoundException>(() => _sut.GetSubmodelRefByIdAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_InvalidBase64_ThrowsInvalidUserInputException()
    {
        const string InvalidEncodedId = "!!invalid_base64@@";
        var request = new GetSubmodelRefRequest(InvalidEncodedId, 5, null);

        await Assert.ThrowsAsync<InvalidUserInputException>(() => _sut.GetSubmodelRefByIdAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetThumbnailAsync_ShouldReturnFileAttachmentResult()
    {
        var expectedResult = new FileAttachmentResult(new MemoryStream(), "image/png", "test.png", 100 * 1024 * 1024);
        _aasRepositoryService.GetThumbnailAsync("https://example.com/aas", Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var request = new GetThumbnailRequest("aHR0cHM6Ly9leGFtcGxlLmNvbS9hYXM");
        var result = await _sut.GetThumbnailAsync(request, CancellationToken.None);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task GetThumbnailAsync_CallsServiceWithDecodedAasIdentifier_WhenInputIsValid()
    {
        const string aasIdentifier = "https://example.com/aas";
        var encodedId = aasIdentifier.EncodeBase64Url();
        var request = new GetThumbnailRequest(encodedId);
        var expectedResult = new FileAttachmentResult(Stream.Null, "image/png", "thumbnail.png", 100 * 1024 * 1024);

        _aasRepositoryService
            .GetThumbnailAsync(aasIdentifier, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        await _sut.GetThumbnailAsync(request, CancellationToken.None);

        await _aasRepositoryService.Received(1)
            .GetThumbnailAsync(aasIdentifier, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetThumbnailAsync_InvalidBase64AasIdentifier_ThrowsInvalidUserInputException()
    {
        const string invalidEncodedId = "!!invalid_base64@@";
        var request = new GetThumbnailRequest(invalidEncodedId);

        await Assert.ThrowsAsync<InvalidUserInputException>(() =>
            _sut.GetThumbnailAsync(request, CancellationToken.None));
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    public async Task GetThumbnailAsync_MaliciousDecodedIdentifier_ThrowsInvalidUserInputException(string maliciousIdentifier)
    {
        var request = new GetThumbnailRequest(maliciousIdentifier.EncodeBase64Url());

        await Assert.ThrowsAsync<InvalidUserInputException>(() =>
            _sut.GetThumbnailAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetThumbnailAsync_ServiceReturnsNull_ThrowsTemplateNotFoundException()
    {
        const string aasIdentifier = "https://example.com/aas";
        var request = new GetThumbnailRequest(aasIdentifier.EncodeBase64Url());

        _aasRepositoryService
            .GetThumbnailAsync(aasIdentifier, Arg.Any<CancellationToken>())!
            .Returns((FileAttachmentResult)null!);

        await Assert.ThrowsAsync<TemplateNotFoundException>(() =>
            _sut.GetThumbnailAsync(request, CancellationToken.None));
    }

    private static AssetInformation CreateAssetInformation()
    {
        var thumbnail = Substitute.For<IResource>();
        thumbnail.Path = "AAS_Logo.svg";
        thumbnail.ContentType = "image/svg+xml";

        return new AssetInformation(
            assetKind: AssetKind.Type,
            globalAssetId: "https://admin-shell.io/idta/asset/ContactInformation/1/0",
            specificAssetIds: [],
            defaultThumbnail: thumbnail
        );
    }

    [Fact]
    public async Task GetSubmodelByAasIdAsync_ReturnsSubmodel_WhenSubmodelBelongsToAas()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        var request = new GetSubmodelByAasRequest(AasId.EncodeBase64Url(), SubmodelId.EncodeBase64Url(), Level.deep, Extent.withoutBlobValue);
        var expectedSubmodel = new Submodel(SubmodelId);
        _aasRepositoryService.GetSubmodelByAasIdAsync(
                AasId,
                SubmodelId,
                Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.deep.ToString() && o.Extent == Extent.withoutBlobValue.ToString()),
                Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelByAasIdAsync(request, CancellationToken.None);

        Assert.IsType<Submodel>(result);
        await _aasRepositoryService.Received(1)
            .GetSubmodelByAasIdAsync(
                AasId,
                SubmodelId,
                Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.deep.ToString() && o.Extent == Extent.withoutBlobValue.ToString()),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelByAasIdAsync_SubmodelNotInAas_ThrowsSubmodelNotFoundException()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        var request = new GetSubmodelByAasRequest(AasId.EncodeBase64Url(), SubmodelId.EncodeBase64Url(), Level.deep, Extent.withoutBlobValue);
        _aasRepositoryService.GetSubmodelByAasIdAsync(
                AasId,
                SubmodelId,
                Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.deep.ToString() && o.Extent == Extent.withoutBlobValue.ToString()),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubmodelNotFoundException(SubmodelId));

        await Assert.ThrowsAsync<SubmodelNotFoundException>(
            () => _sut.GetSubmodelByAasIdAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelByAasIdAsync_AasNotFound_PropagatesException()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        var request = new GetSubmodelByAasRequest(AasId.EncodeBase64Url(), SubmodelId.EncodeBase64Url(), Level.deep, Extent.withoutBlobValue);
        _aasRepositoryService.GetSubmodelByAasIdAsync(
                AasId,
                SubmodelId,
                Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.deep.ToString() && o.Extent == Extent.withoutBlobValue.ToString()),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new TemplateNotFoundException());

        await Assert.ThrowsAsync<TemplateNotFoundException>(
            () => _sut.GetSubmodelByAasIdAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllSubmodelElementsByAasIdAsync_ReturnsList_WhenSubmodelBelongsToAas()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        var request = new GetAllSubmodelElementsByAasRequest(AasId.EncodeBase64Url(), SubmodelId.EncodeBase64Url(), null, null, Level.deep, Extent.withoutBlobValue);
        _aasRepositoryService.GetAllSubmodelElementsByAasIdAsync(
            AasId,
            SubmodelId,
            Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.deep.ToString() && o.Extent == Extent.withoutBlobValue.ToString()),
            null,
            null,
            Arg.Any<CancellationToken>())
            .Returns(new SubmodelElementsPage { PagingMetaData = new PagingMetaData(), Result = [] });

        var result = await _sut.GetAllSubmodelElementsByAasIdAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        await _aasRepositoryService.Received(1)
            .GetAllSubmodelElementsByAasIdAsync(
                AasId,
                SubmodelId,
                Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.deep.ToString() && o.Extent == Extent.withoutBlobValue.ToString()),
                null,
                null,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelElementsByAasIdAsync_SubmodelNotInAas_ThrowsSubmodelNotFoundException()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        var request = new GetAllSubmodelElementsByAasRequest(AasId.EncodeBase64Url(), SubmodelId.EncodeBase64Url(), null, null, Level.deep, Extent.withoutBlobValue);
        _aasRepositoryService.GetAllSubmodelElementsByAasIdAsync(
                AasId,
                SubmodelId,
                Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.deep.ToString() && o.Extent == Extent.withoutBlobValue.ToString()),
                null,
                null,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubmodelNotFoundException(SubmodelId));

        await Assert.ThrowsAsync<SubmodelNotFoundException>(
            () => _sut.GetAllSubmodelElementsByAasIdAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelElementByAasIdAsync_ReturnsElement_WhenSubmodelBelongsToAas()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        const string IdShortPath = "ManufacturerName";
        var request = new GetSubmodelElementByAasRequest(
            AasId.EncodeBase64Url(),
            SubmodelId.EncodeBase64Url(),
            IdShortPath,
            Level.deep,
            Extent.withoutBlobValue);
        var expectedElement = new Property(idShort: IdShortPath, valueType: DataTypeDefXsd.String);
        _aasRepositoryService.GetSubmodelElementByAasIdAsync(AasId, SubmodelId, IdShortPath, Arg.Any<SubmodelQueryOptions>(), Arg.Any<CancellationToken>())
            .Returns(expectedElement);

        var result = await _sut.GetSubmodelElementByAasIdAsync(request, CancellationToken.None);

        Assert.IsType<Property>(result);
        await _aasRepositoryService.Received(1)
            .GetSubmodelElementByAasIdAsync(AasId, SubmodelId, IdShortPath, Arg.Any<SubmodelQueryOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelElementByAasIdAsync_SubmodelNotInAas_ThrowsSubmodelNotFoundException()
    {
        const string AasId = "AasId";
        const string SubmodelId = "SubmodelId";
        const string idShortPath = "SomePath";
        var request = new GetSubmodelElementByAasRequest(
            AasId.EncodeBase64Url(),
            SubmodelId.EncodeBase64Url(),
            idShortPath,
            Level.deep,
            Extent.withoutBlobValue);
        _aasRepositoryService.GetSubmodelElementByAasIdAsync(AasId, SubmodelId, idShortPath, Arg.Any<SubmodelQueryOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SubmodelNotFoundException(SubmodelId));

        await Assert.ThrowsAsync<SubmodelNotFoundException>(
            () => _sut.GetSubmodelElementByAasIdAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelByAasIdAsync_WithCoreAndWithBlobValue_PassesQueryOptionsToService()
    {
        const string aasId = "AasId";
        const string submodelId = "SubmodelId";
        var request = new GetSubmodelByAasRequest(aasId.EncodeBase64Url(), submodelId.EncodeBase64Url(), Level.core, Extent.withBlobValue);
        var expectedSubmodel = new Submodel(submodelId);

        _aasRepositoryService.GetSubmodelByAasIdAsync(
                aasId,
                submodelId,
                Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.core.ToString() && o.Extent == Extent.withBlobValue.ToString()),
                Arg.Any<CancellationToken>())
            .Returns(expectedSubmodel);

        var result = await _sut.GetSubmodelByAasIdAsync(request, CancellationToken.None);

        Assert.Same(expectedSubmodel, result);
        await _aasRepositoryService.Received(1).GetSubmodelByAasIdAsync(
            aasId,
            submodelId,
            Arg.Is<SubmodelQueryOptions>(o => o.Level == Level.core.ToString() && o.Extent == Extent.withBlobValue.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetSubmodelElementByAasIdAsync_InvalidIdShortPath_ThrowsInvalidUserInputException(string idShortPath)
    {
        const string aasId = "AasId";
        const string submodelId = "SubmodelId";
        var request = new GetSubmodelElementByAasRequest(
            aasId.EncodeBase64Url(),
            submodelId.EncodeBase64Url(),
            idShortPath,
            Level.deep,
            Extent.withoutBlobValue);

        await Assert.ThrowsAsync<InvalidUserInputException>(() =>
            _sut.GetSubmodelElementByAasIdAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetFileByPathByAasIdAsync_ReturnsAttachment_WhenInputIsValid()
    {
        const string aasId = "https://example.com/aas/1";
        const string submodelId = "https://example.com/submodels/contact";
        const string idShortPath = "Thumbnail";

        var request = new GetFileByPathByAasIdRequest(
            aasId.EncodeBase64Url(),
            submodelId.EncodeBase64Url(),
            idShortPath);

        var expected = new FileAttachmentResult(Stream.Null, "image/png", "thumbnail.png", 100 * 1024 * 1024);

        _aasRepositoryService.GetFileAttachmentByAasIdAsync(
            aasId,
            submodelId,
            idShortPath,
            Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetFileByPathByAasIdAsync(request, CancellationToken.None);

        Assert.Same(expected, result);
        await _aasRepositoryService.Received(1).GetFileAttachmentByAasIdAsync(
            aasId,
            submodelId,
            idShortPath,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData(@"..\\..\\windows\\system32")]
    [InlineData("element/../otherElement")]
    [InlineData("%2e%2e/config")]
    public async Task GetFileByPathByAasIdAsync_InvalidIdShortPath_ThrowsInvalidUserInputException(string maliciousPath)
    {
        const string aasId = "https://example.com/aas/1";
        const string submodelId = "https://example.com/submodels/contact";

        var request = new GetFileByPathByAasIdRequest(
            aasId.EncodeBase64Url(),
            submodelId.EncodeBase64Url(),
            maliciousPath);

        await Assert.ThrowsAsync<InvalidUserInputException>(() =>
            _sut.GetFileByPathByAasIdAsync(request, CancellationToken.None));
    }
}

