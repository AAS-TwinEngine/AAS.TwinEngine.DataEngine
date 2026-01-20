using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.ApplicationLogic.Services.MetaData;

public class MetaDataServiceTests
{
    private readonly ILogger<MetaDataService> _logger = Substitute.For<ILogger<MetaDataService>>();
    private readonly IMetaDataProvider _repository = Substitute.For<IMetaDataProvider>();
    private readonly MetaDataService _sut;
    private const string AasIdentifier = "ContactInformation";

    public MetaDataServiceTests() => _sut = new MetaDataService(_logger, _repository);

    [Fact]
    public async Task GetShellsAsync_ReturnsShells()
    {
        var expectedShells = new ShellDescriptorsData();
        _repository.GetShellDescriptorsAsync(null, null, Arg.Any<CancellationToken>()).Returns(expectedShells);

        var result = await _sut.GetShellDescriptorsAsync(null, null, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetShellElementAsync_ReturnsShellElement()
    {
        var expectedShell = new ShellDescriptorData();
        _repository.GetShellDescriptorAsync(AasIdentifier, Arg.Any<CancellationToken>())
        .Returns(expectedShell);

        var result = await _sut.GetShellDescriptorAsync(AasIdentifier, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedShell, result);
    }

    [Fact]
    public async Task GetShellElementAsync_ReturnsNull_WhenNotFound()
    {
        _repository.GetShellDescriptorAsync(AasIdentifier, Arg.Any<CancellationToken>())!
                   .Returns((ShellDescriptorData)null!);

        var result = await _sut.GetShellDescriptorAsync(AasIdentifier, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssetElementAsync_ReturnsAssetElement()
    {
        var expectedAsset = new AssetData();
        _repository.GetAssetAsync(AasIdentifier, Arg.Any<CancellationToken>())
                   .Returns(expectedAsset);

        var result = await _sut.GetAssetAsync(AasIdentifier, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedAsset, result);
    }

    [Fact]
    public async Task GetAssetElementAsync_ReturnsNull_WhenNotFound()
    {
        _repository.GetAssetAsync(AasIdentifier, Arg.Any<CancellationToken>())
                   .Returns((AssetData)null!);

        var result = await _sut.GetAssetAsync(AasIdentifier, CancellationToken.None);

        Assert.Null(result);
    }
}
