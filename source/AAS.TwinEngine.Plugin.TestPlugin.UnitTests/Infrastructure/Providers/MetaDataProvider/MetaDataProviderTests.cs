using System.Text.Json;

using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Provider = Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.MetaDataProvider.MetaDataProvider;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.Providers.MetaDataProvider;

public class MetaDataProviderTests
{
    private readonly ILogger<Provider> _logger = Substitute.For<ILogger<Provider>>();
    private readonly Provider _sut;

    public MetaDataProviderTests()
    {
        SetMetaData(TestData.TestMetaData);
        _sut = new Provider(_logger);
    }

    private static void SetMetaData(string jsonContent) => MockData.MetaData = JsonDocument.Parse(jsonContent);

    [Fact]
    public async Task GetShellDescriptorsAsync_ReturnsPagedShells_WithPagingMetadata()
    {
        const int Limit = 2;
        string? cursor = null;

        var result = await _sut.GetShellDescriptorsAsync(Limit, cursor, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.True(result.Result.Count <= Limit);
        Assert.NotNull(result.PagingMetaData);
        Assert.False(string.IsNullOrWhiteSpace(result.PagingMetaData.Cursor));
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_ReturnsCorrectPage_WhenCursorIsProvided()
    {
        var firstPage = await _sut.GetShellDescriptorsAsync(2, null, CancellationToken.None);
        var nextCursor = firstPage.PagingMetaData?.Cursor;

        var secondPage = await _sut.GetShellDescriptorsAsync(2, nextCursor, CancellationToken.None);

        Assert.NotNull(secondPage);
        Assert.NotNull(secondPage.Result);
        Assert.True(secondPage.Result.Count <= 2);
        Assert.NotEqual(firstPage.Result?.FirstOrDefault()?.Id, secondPage.Result?.FirstOrDefault()?.Id);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_ThrowsNotFound_WhenCursorIsInvalid()
    {
        var record = await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetShellDescriptorsAsync(2, "bW0=", CancellationToken.None));

        Assert.Equal(ExceptionMessages.ShellDescriptorDataNotFound, record.Message);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_NeverReturnsShell_WithEmptyIds()
    {
        var result = await _sut.GetShellDescriptorsAsync(null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.All(result.Result!, shell => Assert.False(string.IsNullOrWhiteSpace(shell.Id), "Shell with empty or null Id found."));
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_ReturnsEmptyList_WhenNoShellsExist()
    {
        SetMetaData("[]");
        var sut = new Provider(_logger);

        var result = await sut.GetShellDescriptorsAsync(null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Result ?? []);
        Assert.NotNull(result.PagingMetaData);
        Assert.Null(result.PagingMetaData.Cursor);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_ReturnsCorrectShell()
    {
        const string Id = "ContactInformation";

        var result = await _sut.GetShellDescriptorAsync(Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(Id, result.Id);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_ReturnsCorrectShell_HasEmptyIdShort()
    {
        const string Id = "1000-859";

        var result = await _sut.GetShellDescriptorAsync(Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(Id, result.Id);
        Assert.Empty(result.IdShort);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_ThrowsNotFoundException_WhenIdNotFound()
    {
        const string InvalidId = "nonexistent-id";

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetShellDescriptorAsync(InvalidId, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetAsync_ReturnsAsset_WhenExists()
    {
        const string AssetId = "ContactInformation";

        var result = await _sut.GetAssetAsync(AssetId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AssetId, result.GlobalAssetId);
    }

    [Fact]
    public async Task GetAssetAsync_ThrowsNotFoundException_WhenShellFound_ButDoesNotHaveAssetInformation()
    {
        const string InvalidAssetId = "SoftwareNameplate";

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetAssetAsync(InvalidAssetId, CancellationToken.None));
        Assert.Contains(ExceptionMessages.AssetNotFound, exception.Message, StringComparison.CurrentCulture);
    }

    [Fact]
    public async Task GetAssetAsync_ThrowsNotFoundException_WhenAssetNotFound()
    {
        const string InvalidAssetId = "nonexistent-asset";

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetAssetAsync(InvalidAssetId, CancellationToken.None));
        Assert.Contains(ExceptionMessages.AssetNotFound, exception.Message, StringComparison.CurrentCulture);
    }
}
