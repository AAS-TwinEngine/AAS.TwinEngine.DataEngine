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
    [InlineData("text/csv, application/xml", SerializationFormat.Xml)]
    [InlineData("application/json, application/xml", SerializationFormat.Json)]
    [InlineData("application/xml, application/json", SerializationFormat.Xml)]
    public void Resolve_ReturnsFirstSupportedMediaType_InHeaderOrder(string acceptHeader, SerializationFormat expected)
        => Assert.Equal(expected, SerializationFormatResolver.Resolve(acceptHeader));

    [Theory]
    [InlineData("application/xml;q=0.2, application/json;q=0.8", SerializationFormat.Xml)]
    [InlineData("application/json;q=0, application/xml", SerializationFormat.Json)]
    [InlineData("application/json;q=0.5", SerializationFormat.Json)]
    public void Resolve_IgnoresQualityFactors(string acceptHeader, SerializationFormat expected)
        => Assert.Equal(expected, SerializationFormatResolver.Resolve(acceptHeader));

    [Theory]
    [InlineData("text/csv")]
    [InlineData("text/*")]
    [InlineData("application/pdf, image/png")]
    public void Resolve_Throws_WhenNoSupportedMediaTypeIsAcceptable(string acceptHeader)
    {
        var exception = Assert.Throws<InvalidRequestHeaderException>(() => SerializationFormatResolver.Resolve(acceptHeader));

        Assert.Contains("application/json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("application/xml", exception.Message, StringComparison.Ordinal);
        Assert.Contains("application/asset-administration-shell-package+xml", exception.Message, StringComparison.Ordinal);
    }
}
