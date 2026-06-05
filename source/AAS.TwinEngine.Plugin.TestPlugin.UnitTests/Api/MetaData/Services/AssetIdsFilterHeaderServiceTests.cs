using AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.Services;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.MetaData.Services;

public class AssetIdsFilterHeaderParserTests
{
    private readonly ILogger<AssetIdsFilterHeaderValidation> _logger = Substitute.For<ILogger<AssetIdsFilterHeaderValidation>>();
    private readonly AssetIdsFilterHeaderValidation _sut;

    public AssetIdsFilterHeaderParserTests() => _sut = new AssetIdsFilterHeaderValidation(_logger);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseToDomainModel_ReturnsNull_ForEmptyInput(string? headerValue)
    {
        var result = _sut.ParseToDomainModel(headerValue);

        Assert.Null(result);
    }

    [Fact]
    public void ParseToDomainModel_ReturnsFilter_ForValidSpecificAssetId()
    {
        const string header = "[{\"name\":\"serialNumber\",\"value\":\"SN-4711\"}]";

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        Assert.Single(result!.Identifiers);
        Assert.Equal("serialNumber", result.Identifiers[0].Name);
        Assert.Equal("SN-4711", result.Identifiers[0].Value);
    }

    [Fact]
    public void ParseToDomainModel_ReturnsFilter_ForValidGlobalAssetId()
    {
        const string header = "[{\"name\":\"globalAssetId\",\"value\":\"https://mm-software.com/ids/assets/000-001\"}]";

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        Assert.Single(result!.Identifiers);
        Assert.Equal("globalAssetId", result.Identifiers[0].Name);
        Assert.Equal("https://mm-software.com/ids/assets/000-001", result.Identifiers[0].Value);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("[{\"name\":\"serialNumber\"}]")]
    [InlineData("[{\"value\":\"SN-4711\"}]")]
    [InlineData("[{\"name\":\"\",\"value\":\"SN-4711\"}]")]
    [InlineData("[{\"name\":\"serialNumber\",\"value\":\"\"}]")]
    [InlineData("[{\"name\":\"serialNumber\",\"value\":\"SN-4711\",\"extra\":true}]")]
    public void ParseToDomainModel_ThrowsBadRequestException_ForInvalidHeader(string headerValue)
    {
        Assert.Throws<BadRequestException>(() => _sut.ParseToDomainModel(headerValue));
    }
}
