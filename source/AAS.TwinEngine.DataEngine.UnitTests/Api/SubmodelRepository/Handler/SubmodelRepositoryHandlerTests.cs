using System.Text;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.SubmodelRepository.Handler;
public class SubmodelRepositoryHandlerTests
{
    private readonly ISubmodelRepositoryService _submodelRepository = Substitute.For<ISubmodelRepositoryService>();
    private readonly ILogger<SubmodelRepositoryHandler> _logger = Substitute.For<ILogger<SubmodelRepositoryHandler>>();
    private readonly SubmodelRepositoryHandler _sut;

    public SubmodelRepositoryHandlerTests() => _sut = new SubmodelRepositoryHandler(_logger, _submodelRepository);

    [Fact]
    public async Task HandleSubmodel_ReturnsSubmodel_WhenSubmodelExists()
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(SubmodelId));
        var request = new GetSubmodelRequest(encodedId);
        var expectedSubmodel = Substitute.For<ISubmodel>();
        _submodelRepository.GetSubmodelAsync(SubmodelId, Arg.Any<CancellationToken>()).Returns(expectedSubmodel);

        var result = await _sut.GetSubmodel(request, CancellationToken.None);

        Assert.Equal(expectedSubmodel, result);
    }

    [Fact]
    public async Task HandleSubmodel_SubmodelIsNull_ThrowsSubmodelElementNotFoundException()
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(SubmodelId));
        var request = new GetSubmodelRequest(encodedId);
        _submodelRepository.GetSubmodelAsync(SubmodelId, Arg.Any<CancellationToken>())!.Returns((ISubmodel)null!);

        await Assert.ThrowsAsync<SubmodelElementNotFoundException>(() => _sut.GetSubmodel(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleSubmodelAsync_InvalidBase64SubmodelId_ThrowsInternalDataProcessingException()
    {
        const string InvalidEncodedId = "!!invalid_base64@@";
        var request = new GetSubmodelRequest(InvalidEncodedId);

        await Assert.ThrowsAsync<InvalidUserInputException>(() => _sut.GetSubmodel(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleSubmodelElement_ReturnsSubmodel_WhenSubmodelElementExists()
    {
        const string SubmodelId = "NameplateSubmodel";
        const string IdShortPath = "Segments.LinkedSegment";
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(SubmodelId));
        var request = new GetSubmodelElementRequest(encodedId, IdShortPath);
        var submodelElement = Substitute.For<ISubmodelElement>();
        _submodelRepository.GetSubmodelElementAsync(SubmodelId, IdShortPath, Arg.Any<CancellationToken>()).Returns(submodelElement);

        var result = await _sut.GetSubmodelElement(request, CancellationToken.None);

        Assert.Equal(submodelElement, result);
        await _submodelRepository.Received().GetSubmodelElementAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleSubmodelElement_SubmodelIsNull_ThrowsSubmodelNotFoundException()
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(SubmodelId));
        const string IdShortPath = "Segments.LinkedSegment";
        var request = new GetSubmodelElementRequest(encodedId, IdShortPath);
        _submodelRepository.GetSubmodelElementAsync(SubmodelId, IdShortPath, Arg.Any<CancellationToken>()).Returns((ISubmodelElement)null!);

        await Assert.ThrowsAsync<SubmodelNotFoundException>(() => _sut.GetSubmodelElement(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleSubmodelElement_InvalidBase64SubmodelId_ThrowsInternalDataProcessingException()
    {
        const string InvalidEncodedId = "!!invalid_base64@@";
        const string IdShortPath = "Segments.LinkedSegment";
        var request = new GetSubmodelElementRequest(InvalidEncodedId, IdShortPath);

        await Assert.ThrowsAsync<InvalidUserInputException>(() => _sut.GetSubmodelElement(request, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task HandleSubmodelElement_InvalidSubmodelIdentifier_ThrowsInternalDataProcessingException(string submodelIdentifier)
    {
        var encodedId = submodelIdentifier.EncodeBase64Url();
        const string IdShortPath = "Segments.LinkedSegment";
        var request = new GetSubmodelElementRequest(encodedId, IdShortPath);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                _sut.GetSubmodelElement(request, CancellationToken.None));
        Assert.Equal("Invalid User Input.", exception.Message);
    }
}
