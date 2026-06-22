using System.Text;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using AasCore.Aas3_1;

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
    public async Task HandleSubmodelElement_InvalidSubmodelIdentifier_ThrowsInvalidUserInputException(string submodelIdentifier)
    {
        var encodedId = submodelIdentifier.EncodeBase64Url();
        const string IdShortPath = "Segments.LinkedSegment";
        var request = new GetSubmodelElementRequest(encodedId, IdShortPath);

        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                _sut.GetSubmodelElement(request, CancellationToken.None));
        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("element/../otherElement")]
    [InlineData("%2e%2e/config")]
    public async Task HandleSubmodelElement_PathTraversalInIdShortPath_ThrowsInvalidUserInputException(string maliciousIdShortPath)
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = SubmodelId.EncodeBase64Url();
        var request = new GetSubmodelElementRequest(encodedId, maliciousIdShortPath);

        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                _sut.GetSubmodelElement(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img onerror=alert('xss')>")]
    [InlineData("<svg/onload=alert('xss')>")]
    [InlineData("element<script>alert(1)</script>")]
    public async Task HandleSubmodelElement_XSSInIdShortPath_ThrowsInvalidUserInputException(string maliciousIdShortPath)
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = SubmodelId.EncodeBase64Url();
        var request = new GetSubmodelElementRequest(encodedId, maliciousIdShortPath);

        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                _sut.GetSubmodelElement(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("element'; DROP TABLE--")]
    [InlineData("1 UNION SELECT * FROM users")]
    [InlineData("element; DELETE FROM table")]
    public async Task HandleSubmodelElement_SqlInjectionInIdShortPath_ThrowsInvalidUserInputException(string maliciousIdShortPath)
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = SubmodelId.EncodeBase64Url();
        var request = new GetSubmodelElementRequest(encodedId, maliciousIdShortPath);

        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                _sut.GetSubmodelElement(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("data:text/html,<script>")]
    [InlineData("vbscript:msgbox('xss')")]
    [InlineData("file:///etc/passwd")]
    public async Task HandleSubmodelElement_DangerousProtocolInIdShortPath_ThrowsInvalidUserInputException(string maliciousIdShortPath)
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = SubmodelId.EncodeBase64Url();
        var request = new GetSubmodelElementRequest(encodedId, maliciousIdShortPath);

        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                _sut.GetSubmodelElement(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("element with spaces")]
    [InlineData("element/slash")]
    [InlineData("element\\backslash")]
    [InlineData("element|pipe")]
    [InlineData("element;semicolon")]
    [InlineData("element&ampersand")]
    public async Task HandleSubmodelElement_InvalidCharactersInIdShortPath_ThrowsInvalidUserInputException(string invalidIdShortPath)
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = SubmodelId.EncodeBase64Url();
        var request = new GetSubmodelElementRequest(encodedId, invalidIdShortPath);

        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                _sut.GetSubmodelElement(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("ContactInformation1")]
    [InlineData("ManufacturerName")]
    [InlineData("element.subelement")]
    [InlineData("element.subelement.property")]
    [InlineData("list[0]")]
    [InlineData("element[3].property")]
    [InlineData("collection.item_name")]
    [InlineData("element-with-hyphen")]
    [InlineData("element_with_underscore")]
    public async Task HandleSubmodelElement_ValidIdShortPath_DoesNotThrow(string validIdShortPath)
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = SubmodelId.EncodeBase64Url();
        var request = new GetSubmodelElementRequest(encodedId, validIdShortPath);
        var submodelElement = Substitute.For<ISubmodelElement>();
        _submodelRepository.GetSubmodelElementAsync(SubmodelId, validIdShortPath, Arg.Any<CancellationToken>()).Returns(submodelElement);

        var result = await _sut.GetSubmodelElement(request, CancellationToken.None);

        Assert.Equal(submodelElement, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleSubmodelElement_NullOrEmptyIdShortPath_ThrowsInvalidUserInputException(string? invalidIdShortPath)
    {
        const string SubmodelId = "NameplateSubmodel";
        var encodedId = SubmodelId.EncodeBase64Url();
        var request = new GetSubmodelElementRequest(encodedId, invalidIdShortPath!);

        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                _sut.GetSubmodelElement(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", exception.Message);
    }
}
