using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Shared;

public class SerializationFormatResolverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("*/*")]
    [InlineData("application/*")]
    [InlineData("application/asset-administration-shell-package+xml")]
    [InlineData("APPLICATION/ASSET-ADMINISTRATION-SHELL-PACKAGE+XML")]
    public void Resolve_ReturnsAasxPackage_WhenNoOrWildcardOrPackagePreference(string? acceptHeader)
        => Assert.Equal(SerializationFormat.AasxPackage, SerializationFormatResolver.Resolve(acceptHeader));

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("Application/Json")]
    public void Resolve_ReturnsJson_WhenJsonRequested(string acceptHeader)
        => Assert.Equal(SerializationFormat.Json, SerializationFormatResolver.Resolve(acceptHeader));

    [Theory]
    [InlineData("application/xml")]
    [InlineData("application/xml; charset=utf-8")]
    public void Resolve_ReturnsXml_WhenXmlRequested(string acceptHeader)
        => Assert.Equal(SerializationFormat.Xml, SerializationFormatResolver.Resolve(acceptHeader));

    [Theory]
    [InlineData("application/xml;q=0.2, application/json;q=0.8", SerializationFormat.Json)]
    [InlineData("application/json;q=0.3, application/xml;q=0.7", SerializationFormat.Xml)]
    [InlineData("text/csv, application/xml", SerializationFormat.Xml)]
    [InlineData("application/json, application/xml", SerializationFormat.Json)]
    public void Resolve_HonoursQualityFactors(string acceptHeader, SerializationFormat expected)
        => Assert.Equal(expected, SerializationFormatResolver.Resolve(acceptHeader));

    [Fact]
    public void Resolve_IgnoresMediaTypesWithZeroQuality()
    {
        var format = SerializationFormatResolver.Resolve("application/json;q=0, application/xml");

        Assert.Equal(SerializationFormat.Xml, format);
    }

    [Fact]
    public void Resolve_UsesAnotherSupportedFormat_WhenWildcardIncludesExplicitlyExcludedFormat()
    {
        var format = SerializationFormatResolver.Resolve("application/asset-administration-shell-package+xml;q=0, */*;q=1");

        Assert.Equal(SerializationFormat.Json, format);
    }

    [Theory]
    [InlineData("text/csv")]
    [InlineData("text/*")]
    [InlineData("application/pdf, image/png")]
    [InlineData("*/*;q=0")]
    [InlineData("application/asset-administration-shell-package+xml;q=0, application/json;q=0, application/xml;q=0, */*;q=1")]
    public void Resolve_Throws_WhenNoSupportedMediaTypeIsAcceptable(string acceptHeader)
    {
        var exception = Assert.Throws<InvalidRequestHeaderException>(() => SerializationFormatResolver.Resolve(acceptHeader));

        Assert.Contains("application/json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("application/xml", exception.Message, StringComparison.Ordinal);
        Assert.Contains("application/asset-administration-shell-package+xml", exception.Message, StringComparison.Ordinal);
    }
}
