using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.SubmodelRepository.Handler;

public class SerializationHandlerTests
{
    private readonly ISerializationService _serializationService;
    private readonly SerializationHandler _sut;

    private readonly SerializeAasxRequest _request = new(["dmFsaWRBQVNJRA=="],
                                                         ["dmFsaWRTdWJtb2RlbElE"],
                                                         false);

    public SerializationHandlerTests()
    {
        var logger = Substitute.For<ILogger<SerializationHandler>>();
        _serializationService = Substitute.For<ISerializationService>();
        _sut = new SerializationHandler(logger, _serializationService);
    }

    [Fact]
    public async Task GetAasxFileAsync_ReturnsFileStreamResult_WhenValidRequest()
    {
        var stream = new MemoryStream("dummy content"u8.ToArray());
        _serializationService.GetAasxFileStreamAsync(
                                                     Arg.Is<IList<string>>(ids => ids[0] == "validAASID"),
                                                     Arg.Is<IList<string>>(ids => ids[0] == "validSubmodelID"),
                                                     false,
                                                     Arg.Any<CancellationToken>()).Returns(stream);

        var result = await _sut.GetAasxFileAsync(_request, CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/asset-administration-shell-package+xml", fileResult.ContentType);
        Assert.Equal("validAASID.aasx", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task GetAasxFileAsync_ThrowsInternalDataProcessingException_WhenAasIdsAreNullOrEmpty()
    {
        var request = new SerializeAasxRequest(null!, ["dmFsaWRTdWJtb2RlbElE"], false);

        var ex = await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
                                                                               _sut.GetAasxFileAsync(request, CancellationToken.None));

        Assert.Equal("Internal Server Error.", ex.Message);
    }

    [Fact]
    public async Task GetAasxFileAsync_ThrowsInternalDataProcessingException_WhenSubmodelIdsAreNullOrEmpty()
    {
        var validEncodedAasId = "https://example.com/aas/test".EncodeBase64Url();
        var request = new SerializeAasxRequest([validEncodedAasId], null!, false);

        var ex = await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
                                                                               _sut.GetAasxFileAsync(request, CancellationToken.None));

        Assert.Equal("Internal Server Error.", ex.Message);
    }

    [Fact]
    public async Task GetAasxFileAsync_ThrowsInvalidUserInputException_WhenIdentifierIsEmptyOrWhitespace()
    {
        var request = new SerializeAasxRequest([""], ["dmFsaWRTdWJtb2RlbElE"], false);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetAasxFileAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetAasxFileAsync_ThrowsResourceNotFoundException_WhenStreamIsNull()
    {
        _serializationService.GetAasxFileStreamAsync(Arg.Any<string[]>(),
                                                     Arg.Any<string[]>(),
                                                     true,
                                                     Arg.Any<CancellationToken>())!
                             .Returns((Stream?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                                                                _sut.GetAasxFileAsync(_request, CancellationToken.None));
    }

    [Fact]
    public async Task GetAasxFileAsync_ThrowsInvalidUserInputException_WhenAasIdIsInvalidBase64()
    {
        var request = new SerializeAasxRequest(["not-valid-base64!!!"], ["dmFsaWRTdWJtb2RlbElE"], false);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetAasxFileAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetAasxFileAsync_ThrowsInvalidUserInputException_WhenSubmodelIdIsInvalidBase64()
    {
        var validEncodedAasId = "https://example.com/aas/test".EncodeBase64Url();
        var request = new SerializeAasxRequest([validEncodedAasId], ["not-valid-base64!!!"], false);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetAasxFileAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetAasxFileAsync_ThrowsInvalidUserInputException_WhenDecodedAasIdContainsMaliciousPattern()
    {
        const string MaliciousId = "javascript:alert('xss')";
        var encodedMaliciousId = MaliciousId.EncodeBase64Url();
        var validEncodedSubmodelId = "https://example.com/submodel".EncodeBase64Url();
        var request = new SerializeAasxRequest([encodedMaliciousId], [validEncodedSubmodelId], false);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetAasxFileAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetAasxFileAsync_ThrowsInvalidUserInputException_WhenDecodedSubmodelIdContainsMaliciousPattern()
    {
        var validEncodedAasId = "https://example.com/aas".EncodeBase64Url();
        const string MaliciousId = "<script>alert('xss')</script>";
        var encodedMaliciousId = MaliciousId.EncodeBase64Url();
        var request = new SerializeAasxRequest([validEncodedAasId], [encodedMaliciousId], false);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetAasxFileAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }
}
