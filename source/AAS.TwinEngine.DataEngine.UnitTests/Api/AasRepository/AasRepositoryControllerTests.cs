using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.Api.AasRepository;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.Shared.Results;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.AasRepository;

public class AasRepositoryControllerTests
{
    private readonly IAasRepositoryHandler _handler;
    private readonly AasRepositoryController _sut;
    private readonly IAssetAdministrationShell _expectedShell;
    private readonly JsonObject _expectedShellResponse;
    private readonly IAssetInformation _expectedAssetInformation;
    private readonly JsonObject _expectedAssetInformationResponse;
    private readonly JsonElement _expectedSubmodelRef;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public const string AasIdentifier = "https://example.com/ids/aas/1170_1160_3052_6568";
    private const int Limit = 1;

    public AasRepositoryControllerTests()
    {
        var logger = Substitute.For<ILogger<AasRepositoryController>>();
        _handler = Substitute.For<IAasRepositoryHandler>();
        _sut = new AasRepositoryController(logger, _handler);
        _expectedShell = CreateShell();
        _expectedAssetInformation = CreateAssetInformation();
        _expectedShellResponse = Jsonization.Serialize.ToJsonObject(_expectedShell);
        _expectedAssetInformationResponse = Jsonization.Serialize.ToJsonObject(_expectedAssetInformation);
        _expectedSubmodelRef = JsonSerializer.SerializeToElement(CreateSubmodelRefDto(), _options);
    }

    [Fact]
    public async Task GetShellsByAssetIdAsync_ReturnsOkResult()
    {
        var expectedResponse = new ShellsDto { PagingMetaData = new PagingMetaDataDto { Cursor = null }, Result = [] };
        var request = new GetShellsByAssetIdsRequest(["dGVzdA"], null, 100, null);
        _handler.GetShellsByAssetIdsAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _sut.GetShellsByAssetIdAsync(["dGVzdA"], null, null, CancellationToken.None);

        Assert.IsType<ActionResult<ShellsDto>>(result);
    }

    [Fact]
    public async Task GetShellsByAssetIdAsync_WithIdShort_ReturnsOkResult()
    {
        var expectedResponse = new ShellsDto { PagingMetaData = new PagingMetaDataDto { Cursor = null }, Result = [] };
        const string idShort = "test-idshort";
        _handler.GetShellsByAssetIdsAsync(Arg.Any<GetShellsByAssetIdsRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _sut.GetShellsByAssetIdAsync(["dGVzdA"], idShort, null, CancellationToken.None);

        Assert.IsType<ActionResult<ShellsDto>>(result);
        await _handler.Received(1).GetShellsByAssetIdsAsync(
            Arg.Is<GetShellsByAssetIdsRequest>(r => r.IdShort == idShort),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellsByAssetIdAsync_ThrowsException_Propagates()
    {
        _handler.GetShellsByAssetIdsAsync(Arg.Any<GetShellsByAssetIdsRequest>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("error"));

        var exception = await Record.ExceptionAsync(() => _sut.GetShellsByAssetIdAsync(["dGVzdA"], null, null, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<Exception>(exception);
    }

    [Fact]
    public async Task GetShellByIdAsync_ReturnsOkResult()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();

        _handler.GetShellByIdAsync(Arg.Any<GetShellRequest>(), Arg.Any<CancellationToken>()).Returns(_expectedShell);

        var result = await _sut.GetShellByIdAsync(encodedId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = Assert.IsType<JsonObject>(okResult.Value);
        Assert.Equal(_expectedShellResponse.ToJsonString(), json.ToJsonString());
    }

    [Fact]
    public async Task GetShellByIdAsync_ThrowsUnauthorizedAccessException_Returns401()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();

        _handler.GetShellByIdAsync(Arg.Any<GetShellRequest>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException("Unauthorized"));

        var exception = await Record.ExceptionAsync(() => _sut.GetShellByIdAsync(encodedId, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<UnauthorizedAccessException>(exception);
    }

    [Fact]
    public async Task GetShellByIdAsync_ThrowsException_ReturnsInternalServerError()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();

        _handler.GetShellByIdAsync(Arg.Any<GetShellRequest>(), Arg.Any<CancellationToken>()).Throws(new Exception("Internal error"));

        var exception = await Record.ExceptionAsync(() => _sut.GetShellByIdAsync(encodedId, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<Exception>(exception);
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ReturnsOkResult()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();

        _handler.GetAssetInformationByIdAsync(Arg.Any<GetAssetInformationRequest>(), Arg.Any<CancellationToken>()).Returns(_expectedAssetInformation);

        var result = await _sut.GetAssetInformationByIdAsync(encodedId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = Assert.IsType<JsonObject>(okResult.Value);
        Assert.Equal(_expectedAssetInformationResponse.ToJsonString(), json.ToJsonString());
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ThrowsUnauthorizedAccessException_Returns401()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();

        _handler.GetAssetInformationByIdAsync(Arg.Any<GetAssetInformationRequest>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException("Unauthorized"));

        var exception = await Record.ExceptionAsync(() => _sut.GetAssetInformationByIdAsync(encodedId, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<UnauthorizedAccessException>(exception);
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ThrowsException_ReturnsInternalServerError()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();
        _handler.GetSubmodelRefByIdAsync(Arg.Any<GetSubmodelRefRequest>(), Arg.Any<CancellationToken>()).Throws(new Exception("Internal error"));

        _handler.GetAssetInformationByIdAsync(Arg.Any<GetAssetInformationRequest>(), Arg.Any<CancellationToken>()).Throws(new Exception("Internal error"));

        var exception = await Record.ExceptionAsync(() => _sut.GetAssetInformationByIdAsync(encodedId, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<Exception>(exception);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ReturnsOkResult()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();
        _handler.GetSubmodelRefByIdAsync(Arg.Any<GetSubmodelRefRequest>(), Arg.Any<CancellationToken>())
            .Returns(_expectedSubmodelRef);

        var result = await _sut.GetSubmodelRefByIdAsync(encodedId, null, CancellationToken.None, Limit);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualJson = JsonSerializer.Serialize(okResult.Value, _options);
        var expectedJson = JsonSerializer.Serialize(_expectedSubmodelRef, _options);
        Assert.Equal(expectedJson, actualJson);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsUnauthorizedAccessException_Returns401()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();
        _handler.GetSubmodelRefByIdAsync(Arg.Any<GetSubmodelRefRequest>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException("Unauthorized"));

        var exception = await Record.ExceptionAsync(() => _sut.GetSubmodelRefByIdAsync(encodedId, null, CancellationToken.None, Limit));

        Assert.NotNull(exception);
        Assert.IsType<UnauthorizedAccessException>(exception);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsException_ReturnsInternalServerError()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();
        _handler.GetSubmodelRefByIdAsync(Arg.Any<GetSubmodelRefRequest>(), Arg.Any<CancellationToken>()).Throws(new Exception("Internal error"));

        var exception = await Record.ExceptionAsync(() => _sut.GetSubmodelRefByIdAsync(encodedId, null, CancellationToken.None, Limit));

        Assert.NotNull(exception);
        Assert.IsType<Exception>(exception);
    }

    private static AssetAdministrationShell CreateShell()
    {
        IReference submodelRef = new Reference(
            type: ReferenceTypes.ModelReference,
            keys:
            [
            new Key(KeyTypes.Submodel, "urn:uuid:submodel-123")
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

    private static IAssetInformation CreateAssetInformation()
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

    private static SubmodelRefDto CreateSubmodelRefDto()
    {
        var key = new Key
        (
            KeyTypes.Submodel,
            "urn:uuid:submodel-123"
        );

        var submodelRef = new Reference(
                                        ReferenceTypes.ModelReference,
                                        [key],
                                        null
                                       );

        return new SubmodelRefDto
        {
            PagingMetaData = null,
            Result = [submodelRef]
        };
    }

    [Fact]
    public async Task GetThumbnailAsync_ShouldReturnFileContentStreamResult_WhenHandlerCompletes()
    {
        var expectedStream = new MemoryStream("test-image"u8.ToArray());
        var attachmentResult = new FileAttachmentResult(expectedStream, "image/png", "thumbnail.png", 100 * 1024 * 1024);
        _handler.GetThumbnailAsync(Arg.Any<GetThumbnailRequest>(), Arg.Any<CancellationToken>())
            .Returns(attachmentResult);

        var response = await _sut.GetThumbnailAsync(AasIdentifier, CancellationToken.None);

        Assert.IsType<FileContentStreamResult>(response);
    }
    [Fact]
    public async Task GetSubmodelByAasIdAsync_ReturnsOkWithJson()
    {
        var encodedAasId = AasIdentifier.EncodeBase64Url();
        const string SubmodelId = "SubmodelId";
        var encodedSubmodelId = SubmodelId.EncodeBase64Url();
        var submodel = new Submodel(SubmodelId);
        _handler.GetSubmodelByAasIdAsync(Arg.Any<GetSubmodelByAasRequest>(), Arg.Any<CancellationToken>())
            .Returns(submodel);

        var result = await _sut.GetSubmodelByAasIdAsync(encodedAasId, encodedSubmodelId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<JsonObject>(okResult.Value);
        await _handler.Received(1).GetSubmodelByAasIdAsync(
            Arg.Is<GetSubmodelByAasRequest>(r => r.AasIdentifier == encodedAasId && r.SubmodelId == encodedSubmodelId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllSubmodelElementsByAasIdAsync_ReturnsOk()
    {
        var encodedAasId = AasIdentifier.EncodeBase64Url();
        const string SubmodelId = "SubmodelId";
        var encodedSubmodelId = SubmodelId.EncodeBase64Url();
        var expected = new SubmodelElementsDto { PagingMetaData = new PagingMetaDataDto(), Result = [] };
        _handler.GetAllSubmodelElementsByAasIdAsync(Arg.Any<GetAllSubmodelElementsByAasRequest>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetAllSubmodelElementsByAasIdAsync(encodedAasId, encodedSubmodelId, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<SubmodelElementsDto>(okResult.Value);
        await _handler.Received(1).GetAllSubmodelElementsByAasIdAsync(
            Arg.Is<GetAllSubmodelElementsByAasRequest>(r => r.AasIdentifier == encodedAasId && r.SubmodelId == encodedSubmodelId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelElementByAasIdAsync_ReturnsOkWithJson()
    {
        var encodedAasId = AasIdentifier.EncodeBase64Url();
        const string SubmodelId = "SubmodelId";
        var encodedSubmodelId = SubmodelId.EncodeBase64Url();
        const string IdShortPath = "ManufacturerName";
        var element = new Property(idShort: IdShortPath, valueType: DataTypeDefXsd.String);
        _handler.GetSubmodelElementByAasIdAsync(Arg.Any<GetSubmodelElementByAasRequest>(), Arg.Any<CancellationToken>())
            .Returns(element);

        var result = await _sut.GetSubmodelElementByAasIdAsync(encodedAasId, encodedSubmodelId, IdShortPath, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<JsonObject>(okResult.Value);
        await _handler.Received(1).GetSubmodelElementByAasIdAsync(
            Arg.Is<GetSubmodelElementByAasRequest>(r => r.AasIdentifier == encodedAasId && r.SubmodelId == encodedSubmodelId && r.IdShortPath == IdShortPath),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFileByPathByAasIdAsync_ReturnsFileContentStreamResult()
    {
        var encodedAasId = AasIdentifier.EncodeBase64Url();
        const string SubmodelId = "SubmodelId";
        var encodedSubmodelId = SubmodelId.EncodeBase64Url();
        const string IdShortPath = "Thumbnail";

        _handler.GetFileByPathByAasIdAsync(Arg.Any<GetFileByPathByAasIdRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FileAttachmentResult(Stream.Null, "image/png", "logo.png", 100 * 1024 * 1024));

        var result = await _sut.GetFileByPathByAasIdAsync(encodedAasId, encodedSubmodelId, IdShortPath, CancellationToken.None);

        Assert.IsType<FileContentStreamResult>(result);
        await _handler.Received(1).GetFileByPathByAasIdAsync(
            Arg.Is<GetFileByPathByAasIdRequest>(r => r.AasIdentifier == encodedAasId && r.SubmodelIdentifier == encodedSubmodelId && r.IdShortPath == IdShortPath),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetThumbnailAsync_PassesRouteValuesToHandler()
    {
        _handler.GetThumbnailAsync(Arg.Any<GetThumbnailRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FileAttachmentResult(Stream.Null, "image/png", "thumbnail.png", 100 * 1024 * 1024));

        await _sut.GetThumbnailAsync(AasIdentifier, CancellationToken.None);

        await _handler.Received(1)
            .GetThumbnailAsync(
                Arg.Is<GetThumbnailRequest>(r => r.AasIdentifier == AasIdentifier),
                Arg.Any<CancellationToken>());
    }
}


