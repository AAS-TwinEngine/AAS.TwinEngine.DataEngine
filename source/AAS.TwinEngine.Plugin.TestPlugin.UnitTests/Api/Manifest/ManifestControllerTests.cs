using Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest;
using Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest.Handler;
using Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest.Responses;

using Microsoft.AspNetCore.Mvc;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Manifest;

public class ManifestControllerTests
{
    private readonly IManifestHandler _manifestHandler = Substitute.For<IManifestHandler>();
    private readonly ManifestController _sut;

    public ManifestControllerTests() => _sut = new ManifestController(_manifestHandler);

    [Fact]
    public async Task RetrieveManifestDataAsync_ShouldReturnOk_WhenDataIsAvailable()
    {
        var expectedManifest = new ManifestDto { Capabilities = new CapabilitiesDto(), SupportedSemanticIds = ["abc"] };
        _manifestHandler.GetManifestData(Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(expectedManifest));

        var result = await _sut.RetrieveManifestDataAsync(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedManifest, okResult.Value);
    }
}
