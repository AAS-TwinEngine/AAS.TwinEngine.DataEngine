using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.MetaDataProvider.Helper;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.Providers.MetaDataProvider.Helper;

public class AssetIdMatcherTests
{
    [Fact]
    public void MatchesAllIdentifiers_ReturnsTrue_WhenFilterIsEmpty()
    {
        var shell = new ShellDescriptorData { Id = "shell-1" };
        var filter = new AssetIdFilterHeader { Identifiers = [] };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.True(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_ReturnsTrue_WhenAllIdentifiersMatch()
    {
        var shell = new ShellDescriptorData
        {
            Id = "shell-1",
            GlobalAssetId = "https://mm-software.com/ids/assets/000-001",
            SpecificAssetIds =
            [
                new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN-4711" },
                new SpecificAssetIdsData { Name = "assetType", Value = "Demo" }
            ]
        };

        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "https://mm-software.com/ids/assets/000-001" },
                new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN-4711" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.True(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_ReturnsFalse_WhenAnyIdentifierDoesNotMatch()
    {
        var shell = new ShellDescriptorData
        {
            Id = "shell-1",
            GlobalAssetId = "https://mm-software.com/ids/assets/000-001",
            SpecificAssetIds =
            [
                new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN-4711" }
            ]
        };

        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN-4711" },
                new SpecificAssetIdsData { Name = "assetType", Value = "Missing" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.False(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_ReturnsFalse_WhenGlobalAssetIdDoesNotMatch()
    {
        var shell = new ShellDescriptorData
        {
            Id = "shell-1",
            GlobalAssetId = "https://mm-software.com/ids/assets/000-001"
        };

        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "https://mm-software.com/ids/assets/other" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.False(result);
    }

    [Fact]
    public void MatchesAllIdentifiers_ReturnsFalse_WhenSpecificAssetIdDoesNotMatch()
    {
        var shell = new ShellDescriptorData
        {
            Id = "shell-1",
            SpecificAssetIds =
            [
                new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN-4711" }
            ]
        };

        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN-9999" }
            ]
        };

        var result = AssetIdMatcher.MatchesAllIdentifiers(shell, filter);

        Assert.False(result);
    }
}
