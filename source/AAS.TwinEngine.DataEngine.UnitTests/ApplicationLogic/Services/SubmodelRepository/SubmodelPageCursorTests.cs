using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository;

public class SubmodelPageCursorTests
{
    [Fact]
    public void EncodeThenTryDecode_RoundTripsAllFields()
    {
        var cursor = new SubmodelPageCursor("PLUGIN-PAGE-2", "AAS-1", "SM-2");

        var decoded = SubmodelPageCursor.TryDecode(cursor.Encode());

        Assert.NotNull(decoded);
        Assert.Equal("PLUGIN-PAGE-2", decoded!.PluginPageCursor);
        Assert.Equal("AAS-1", decoded.CurrentAasId);
        Assert.Equal("SM-2", decoded.LastSubmodelId);
    }

    [Fact]
    public void EncodeThenTryDecode_WithNullPluginPageCursor_RoundTripsAsNull()
    {
        var cursor = new SubmodelPageCursor(null, "AAS-1", "SM-2");

        var decoded = SubmodelPageCursor.TryDecode(cursor.Encode());

        Assert.NotNull(decoded);
        Assert.Null(decoded!.PluginPageCursor);
        Assert.Equal("AAS-1", decoded.CurrentAasId);
        Assert.Equal("SM-2", decoded.LastSubmodelId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryDecode_WithEmptyOrWhitespace_ReturnsNull(string? encodedCursor) => Assert.Null(SubmodelPageCursor.TryDecode(encodedCursor));

    [Fact]
    public void TryDecode_WithWrongFieldCount_ReturnsNull()
    {
        // Only two fields instead of the expected three.
        var malformed = "AAS-1|SM-2".EncodeBase64Url();

        Assert.Null(SubmodelPageCursor.TryDecode(malformed));
    }
}
