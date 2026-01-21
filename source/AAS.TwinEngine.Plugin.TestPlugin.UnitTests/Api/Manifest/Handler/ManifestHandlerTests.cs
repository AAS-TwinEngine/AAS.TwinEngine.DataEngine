using AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.Handler;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.Responses;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Manifest.Handler;

public class ManifestHandlerTests
{
    private readonly ILogger<ManifestHandler> _logger = Substitute.For<ILogger<ManifestHandler>>();
    private readonly IManifestService _manifestService = Substitute.For<IManifestService>();
    private readonly ManifestHandler _sut;

    public ManifestHandlerTests() => _sut = new ManifestHandler(_logger, _manifestService);

    [Fact]
    public async Task GetManifestData_ShouldReturnDto_WhenManifestIsAvailable()
    {
        var manifest = new ManifestData { Capabilities = new CapabilitiesData { HasAssetInformation = true, HasShellDescriptor = true }, SupportedSemanticIds = ["test"] };
        var expectedDto = new ManifestDto { Capabilities = new CapabilitiesDto { HasAssetInformation = true, HasShellDescriptor = true }, SupportedSemanticIds = ["test"] };
        _manifestService.GetManifestData(Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(manifest));

        var result = await _sut.GetManifestData(CancellationToken.None);

        Assert.Equal(expectedDto.ToString(), result.ToString());
    }

    [Fact]
    public async Task GetManifestData_ShouldThrowException_WhenServiceThrows()
    {
        _manifestService.GetManifestData(Arg.Any<CancellationToken>())
                        .Throws(new Exception("Service failure"));

        await Assert.ThrowsAsync<Exception>(() => _sut.GetManifestData(CancellationToken.None));
    }
}
