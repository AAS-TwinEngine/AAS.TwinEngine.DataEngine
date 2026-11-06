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

    private readonly SerializationRequest _request = new(["dmFsaWRBQVNJRA=="],
                                                         ["dmFsaWRTdWJtb2RlbElE"],
                                                         false,
                                                         null);

    public SerializationHandlerTests()
    {
        var logger = Substitute.For<ILogger<SerializationHandler>>();
        _serializationService = Substitute.For<ISerializationService>();
        _sut = new SerializationHandler(logger, _serializationService);
    }

    [Fact]
    public async Task GetSerializationAsync_ReturnsAasxFileStreamResult_WhenAcceptHeaderIsMissing()
    {
        var stream = new MemoryStream("dummy content"u8.ToArray());
        _serializationService.GetSerializationStreamAsync(
                                                          Arg.Is<IList<string>>(ids => ids[0] == "validAASID"),
                                                          Arg.Is<IList<string>>(ids => ids[0] == "validSubmodelID"),
                                                          false,
                                                          SerializationFormat.AasxPackage,
                                                          Arg.Any<CancellationToken>()).Returns(stream);

        var result = await _sut.GetSerializationAsync(_request, CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/asset-administration-shell-package+xml", fileResult.ContentType);
        Assert.Equal("validAASID.aasx", fileResult.FileDownloadName);
    }

    [Theory]
    [InlineData("application/asset-administration-shell-package+xml", SerializationFormat.AasxPackage, "application/asset-administration-shell-package+xml", "validAASID.aasx")]
    [InlineData("application/json", SerializationFormat.Json, "application/json", "validAASID.json")]
    [InlineData("application/xml", SerializationFormat.Xml, "application/xml", "validAASID.xml")]
    [InlineData("*/*", SerializationFormat.AasxPackage, "application/asset-administration-shell-package+xml", "validAASID.aasx")]
    [InlineData("application/xml;q=0.5, application/json;q=0.9", SerializationFormat.Json, "application/json", "validAASID.json")]
    public async Task GetSerializationAsync_SelectsFormat_BasedOnAcceptHeader(string acceptHeader,
                                                                              SerializationFormat expectedFormat,
                                                                              string expectedContentType,
                                                                              string expectedFileName)
    {
        var stream = new MemoryStream("dummy content"u8.ToArray());
        var request = _request with { AcceptHeader = acceptHeader };

        _serializationService.GetSerializationStreamAsync(Arg.Any<IList<string>>(),
                                                          Arg.Any<IList<string>>(),
                                                          false,
                                                          expectedFormat,
                                                          Arg.Any<CancellationToken>()).Returns(stream);

        var result = await _sut.GetSerializationAsync(request, CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(expectedContentType, fileResult.ContentType);
        Assert.Equal(expectedFileName, fileResult.FileDownloadName);

        await _serializationService.Received(1)
                                   .GetSerializationStreamAsync(Arg.Any<IList<string>>(),
                                                                Arg.Any<IList<string>>(),
                                                                false,
                                                                expectedFormat,
                                                                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSerializationAsync_ThrowsInvalidRequestHeaderException_WhenAcceptHeaderIsNotSupported()
    {
        var request = _request with { AcceptHeader = "text/csv" };

        var ex = await Assert.ThrowsAsync<InvalidRequestHeaderException>(() =>
                                                                             _sut.GetSerializationAsync(request, CancellationToken.None));

        Assert.Contains("application/asset-administration-shell-package+xml", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSerializationAsync_ThrowsInternalDataProcessingException_WhenAasIdsAreNullOrEmpty()
    {
        var request = new SerializationRequest(null!, ["dmFsaWRTdWJtb2RlbElE"], false, null);

        var ex = await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
                                                                               _sut.GetSerializationAsync(request, CancellationToken.None));

        Assert.Equal("Internal Server Error.", ex.Message);
    }

    [Fact]
    public async Task GetSerializationAsync_ThrowsInternalDataProcessingException_WhenSubmodelIdsAreNullOrEmpty()
    {
        var validEncodedAasId = "https://example.com/aas/test".EncodeBase64Url();
        var request = new SerializationRequest([validEncodedAasId], null!, false, null);

        var ex = await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
                                                                               _sut.GetSerializationAsync(request, CancellationToken.None));

        Assert.Equal("Internal Server Error.", ex.Message);
    }

    [Fact]
    public async Task GetSerializationAsync_ThrowsInvalidUserInputException_WhenIdentifierIsEmptyOrWhitespace()
    {
        var request = new SerializationRequest([""], ["dmFsaWRTdWJtb2RlbElE"], false, null);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetSerializationAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetSerializationAsync_ThrowsResourceNotFoundException_WhenStreamIsNull()
    {
        _serializationService.GetSerializationStreamAsync(Arg.Any<string[]>(),
                                                          Arg.Any<string[]>(),
                                                          true,
                                                          Arg.Any<SerializationFormat>(),
                                                          Arg.Any<CancellationToken>())!
                             .Returns((Stream?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                                                                _sut.GetSerializationAsync(_request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSerializationAsync_ThrowsInvalidUserInputException_WhenAasIdIsInvalidBase64()
    {
        var request = new SerializationRequest(["not-valid-base64!!!"], ["dmFsaWRTdWJtb2RlbElE"], false, null);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetSerializationAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetSerializationAsync_ThrowsInvalidUserInputException_WhenSubmodelIdIsInvalidBase64()
    {
        var validEncodedAasId = "https://example.com/aas/test".EncodeBase64Url();
        var request = new SerializationRequest([validEncodedAasId], ["not-valid-base64!!!"], false, null);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetSerializationAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetSerializationAsync_ThrowsInvalidUserInputException_WhenDecodedAasIdContainsMaliciousPattern()
    {
        const string MaliciousId = "javascript:alert('xss')";
        var encodedMaliciousId = MaliciousId.EncodeBase64Url();
        var validEncodedSubmodelId = "https://example.com/submodel".EncodeBase64Url();
        var request = new SerializationRequest([encodedMaliciousId], [validEncodedSubmodelId], false, null);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetSerializationAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }

    [Fact]
    public async Task GetSerializationAsync_ThrowsInvalidUserInputException_WhenDecodedSubmodelIdContainsMaliciousPattern()
    {
        var validEncodedAasId = "https://example.com/aas".EncodeBase64Url();
        const string MaliciousId = "<script>alert('xss')</script>";
        var encodedMaliciousId = MaliciousId.EncodeBase64Url();
        var request = new SerializationRequest([validEncodedAasId], [encodedMaliciousId], false, null);

        var ex = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                         _sut.GetSerializationAsync(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", ex.Message);
    }
}
