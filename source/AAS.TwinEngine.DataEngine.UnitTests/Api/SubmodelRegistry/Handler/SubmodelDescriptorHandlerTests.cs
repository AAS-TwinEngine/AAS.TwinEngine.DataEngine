using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.SubmodelRegistry.Handler;

public class SubmodelDescriptorHandlerTests
{
    private readonly ISubmodelDescriptorService _submodelDescriptorService = Substitute.For<ISubmodelDescriptorService>();
    private readonly ILogger<SubmodelDescriptorHandler> _logger = Substitute.For<ILogger<SubmodelDescriptorHandler>>();
    private readonly SubmodelDescriptorHandler _sut;

    public SubmodelDescriptorHandlerTests() => _sut = new SubmodelDescriptorHandler(_logger, _submodelDescriptorService);

    [Fact]
    public async Task GetSubmodelDescriptorById_ReturnsDto_WhenDescriptorExists()
    {
        const string Id = "submodelId";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetSubmodelDescriptorRequest(encodedId);
        var descriptor = Substitute.For<SubmodelDescriptor>();
        _submodelDescriptorService.GetSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())
                                  .Returns(descriptor);

        var result = await _sut.GetSubmodelDescriptorById(request, CancellationToken.None);

        Assert.IsType<SubmodelDescriptorDto>(result);
        await _submodelDescriptorService.Received(1).GetSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelDescriptorById_ThrowsSubmodelDescriptorNotFoundException_WhenDescriptorIsNull()
    {
        const string Id = "submodelId";
        var encodedId = Id.EncodeBase64Url();
        var request = new GetSubmodelDescriptorRequest(encodedId);

        _submodelDescriptorService.GetSubmodelDescriptorByIdAsync(Id, Arg.Any<CancellationToken>())
                                  .Returns((SubmodelDescriptor)null!);

        await Assert.ThrowsAsync<SubmodelDescriptorNotFoundException>(() => _sut.GetSubmodelDescriptorById(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelDescriptorById_ThrowsInvalidUserInputException_WhenBase64IsInvalidId()
    {
        const string InValidIdEncodedId = "!!invalidId_base64@@";
        var request = new GetSubmodelDescriptorRequest(InValidIdEncodedId);

        await Assert.ThrowsAsync<InvalidUserInputException>(() => _sut.GetSubmodelDescriptorById(request, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetSubmodelDescriptorRequest_InvalidSubmodelIdentifier_ThrowsInvalidUserInputException(string submodelIdentifier)
    {
        var encodedId = submodelIdentifier.EncodeBase64Url();
        var request = new GetSubmodelDescriptorRequest(encodedId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidUserInputException>(() =>
                                                                                      _sut.GetSubmodelDescriptorById(request, CancellationToken.None));

        Assert.Equal("Invalid User Input.", exception.Message);
    }
}
