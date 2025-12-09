using AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.AasRepository.Handler;

public class AasRepositoryHandlerTests
{
    private readonly IAasRepositoryService _aasRepositoryService = Substitute.For<IAasRepositoryService>();
    private readonly ILogger<AasRepositoryHandler> _logger = Substitute.For<ILogger<AasRepositoryHandler>>();
    private readonly AasRepositoryHandler _sut;

    public AasRepositoryHandlerTests() => _sut = new AasRepositoryHandler(_logger, _aasRepositoryService);

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
}

