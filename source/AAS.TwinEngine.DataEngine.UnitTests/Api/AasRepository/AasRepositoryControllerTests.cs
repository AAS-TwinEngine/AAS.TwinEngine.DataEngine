using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.Api.AasRepository;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using AasCore.Aas3_0;

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

        var result = await _sut.GetSubmodelRefByIdAsync(encodedId, Limit, null, CancellationToken.None);

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

        var exception = await Record.ExceptionAsync(() => _sut.GetSubmodelRefByIdAsync(encodedId, Limit, null, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<UnauthorizedAccessException>(exception);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsException_ReturnsInternalServerError()
    {
        var encodedId = AasIdentifier.EncodeBase64Url();
        _handler.GetSubmodelRefByIdAsync(Arg.Any<GetSubmodelRefRequest>(), Arg.Any<CancellationToken>()).Throws(new Exception("Internal error"));

        var exception = await Record.ExceptionAsync(() => _sut.GetSubmodelRefByIdAsync(encodedId, Limit, null, CancellationToken.None));

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
            Result = new List<IReference> { submodelRef }
        };
    }

}

